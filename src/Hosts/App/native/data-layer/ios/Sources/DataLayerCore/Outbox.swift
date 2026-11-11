import Foundation
import GRDB

public struct OutboxEntry {
    public let id: String
    public let method: String
    public let createdAt: Int64
    public let attempts: Int
    public let lastError: String?
    public let failed: Bool
    public let waitsForUpload: Bool
    public let upload: UploadInfo?
}

public struct UploadInfo {
    public let file: String
    public let fileName: String
    public let bytes: Int64
}

public struct QueuedCall {
    public let method: String
    public let request: Data

    public init(method: String, request: Data) {
        self.method = method
        self.request = request
    }
}

public struct QueuedUpload {
    public let file: String
    public let fileName: String
    public let contentType: String
    public let bytes: Int64
    public let find: QueuedCall
    public let begin: QueuedCall
    public let complete: QueuedCall
    public let paths: [[Int]]

    public init(file: String, fileName: String, contentType: String, bytes: Int64,
                find: QueuedCall, begin: QueuedCall, complete: QueuedCall, paths: [[Int]]) {
        self.file = file
        self.fileName = fileName
        self.contentType = contentType
        self.bytes = bytes
        self.find = find
        self.begin = begin
        self.complete = complete
        self.paths = paths
    }
}

public protocol FileSender: Sendable {
    func put(_ file: URL, to url: URL, contentType: String) async throws -> Int
}

public struct SessionFileSender: FileSender {
    private let session: URLSession

    public init(session: URLSession = .shared) {
        self.session = session
    }

    public func put(_ file: URL, to url: URL, contentType: String) async throws -> Int {
        var request = URLRequest(url: url)
        request.httpMethod = "PUT"
        request.setValue(contentType, forHTTPHeaderField: "Content-Type")
        let (_, response) = try await session.upload(for: request, fromFile: file)
        return (response as? HTTPURLResponse)?.statusCode ?? 0
    }
}

public actor Outbox {
    public static let requestIdHeader = "gsbc-request-id"

    private static let giveUpAfterMilliseconds: Int64 = 3 * 24 * 60 * 60 * 1000
    private static let maximumBackoffSeconds: Double = 60 * 60
    private static let retriedStatuses: Set<Int> = [2, 4, 8, 10, 13, 14]
    private static let notFound = 5
    private static let failedPrecondition = 9
    private static let uploadLinkMargin: Int64 = 14 * 60 * 1000
    private static let stepsPerAttempt = 8

    private let database: DatabaseQueue
    private let transport: GrpcWebTransport
    private let shelf: UploadShelf?
    private let files: FileSender
    private let onChanged: @Sendable () -> Void
    private var draining = false
    private var wake: Task<Void, Never>?

    public init(database: DatabaseQueue, transport: GrpcWebTransport, shelf: UploadShelf? = nil,
                files: FileSender = SessionFileSender(), onChanged: @escaping @Sendable () -> Void) {
        self.database = database
        self.transport = transport
        self.shelf = shelf
        self.files = files
        self.onChanged = onChanged
    }

    public func enqueue(method: String, request: Data, quiet: Bool = false, uploads: [QueuedUpload] = []) throws -> String {
        let id = UUID().uuidString
        let now = Self.now()
        if let missing = uploads.first(where: { shelf?.has($0.file) != true }) {
            throw ProtoError(reason: "\(missing.fileName) is no longer on this phone. Choose it again.")
        }
        try database.write { db in
            var body = request
            var waits: [(upload: String, path: [Int], done: Bool)] = []
            for upload in uploads {
                let (uploadId, mediaId) = try Self.uploadFor(upload, db, now)
                for path in upload.paths {
                    if let mediaId { body = UploadMessages.splice(mediaId, at: path, into: body) }
                    waits.append((uploadId, path, mediaId != nil))
                }
            }
            try db.execute(sql: "INSERT INTO outbox (id, method, request, created_at, next_attempt_at, quiet) VALUES (?, ?, ?, ?, ?, ?)",
                           arguments: [id, method, body, now, now, quiet])
            for wait in waits {
                try db.execute(sql: "INSERT OR IGNORE INTO outbox_waits (entry_id, upload_id, path, done) VALUES (?, ?, ?, ?)",
                               arguments: [id, wait.upload, Self.text(wait.path), wait.done])
            }
        }
        if !quiet {
            onChanged()
        }
        Task { await drain() }
        return id
    }

    public func entries() throws -> [OutboxEntry] {
        try database.read { db in
            try Row.fetchAll(db, sql: """
                SELECT o.id, o.method, o.created_at, o.attempts, o.last_error, o.failed, u.file, u.file_name, u.bytes,
                       EXISTS (SELECT 1 FROM outbox_waits w WHERE w.entry_id = o.id AND w.done = 0) AS waits
                FROM outbox o LEFT JOIN uploads u ON o.kind = 'upload' AND u.id = o.id
                WHERE o.quiet = 0 ORDER BY o.created_at
                """).map { row in
                let file: String? = row["file"]
                return OutboxEntry(id: row["id"], method: row["method"], createdAt: row["created_at"],
                                   attempts: row["attempts"], lastError: row["last_error"], failed: row["failed"],
                                   waitsForUpload: row["waits"],
                                   upload: file.map { UploadInfo(file: $0, fileName: row["file_name"], bytes: row["bytes"]) })
            }
        }
    }

    public func retry(id: String) throws {
        let now = Self.now()
        try database.write { db in
            let linked = try String.fetchAll(db, sql: """
                SELECT upload_id FROM outbox_waits WHERE entry_id = ? AND done = 0
                UNION SELECT entry_id FROM outbox_waits WHERE upload_id = ? AND done = 0
                """, arguments: [id, id])
            for each in [id] + linked {
                try db.execute(sql: "UPDATE outbox SET failed = 0, attempts = 0, created_at = ?, next_attempt_at = ? WHERE id = ?",
                               arguments: [now, now, each])
            }
        }
        onChanged()
        Task { await drain() }
    }

    public func discard(id: String) throws {
        let forgotten = try database.write { db -> [String] in
            let dependents = try String.fetchAll(db, sql: "SELECT DISTINCT entry_id FROM outbox_waits WHERE upload_id = ? AND done = 0", arguments: [id])
            var forgotten: [String] = []
            for dependent in dependents {
                forgotten += try Self.remove(dependent, db)
            }
            return forgotten + (try Self.remove(id, db))
        }
        forgotten.forEach { shelf?.forget($0) }
        onChanged()
    }

    public func discardQuiet() throws {
        try database.write { db in try db.execute(sql: "DELETE FROM outbox WHERE quiet = 1") }
    }

    public static func keptFiles(in database: DatabaseQueue) throws -> Set<String> {
        try database.read { db in Set(try String.fetchAll(db, sql: "SELECT file FROM uploads")) }
    }

    public func isKept(_ file: String) throws -> Bool {
        try database.read { db in try Self.isKept(file, db) }
    }

    public func sendWaitingNow() async {
        let now = Self.now()
        try? await database.write { db in
            try db.execute(sql: "UPDATE outbox SET next_attempt_at = ? WHERE failed = 0 AND next_attempt_at > ?", arguments: [now, now])
        }
        await drain()
    }

    public func drain() async {
        guard !draining else { return }
        draining = true
        defer { draining = false }

        while let entry = try? nextDue() {
            await send(entry)
        }
        scheduleWake()
    }

    private func nextDue() throws -> DueEntry? {
        try database.read { db in
            try Row.fetchOne(db, sql: """
                SELECT id, method, request, created_at, attempts, quiet, kind FROM outbox o
                WHERE failed = 0 AND next_attempt_at <= ?
                  AND NOT EXISTS (SELECT 1 FROM outbox_waits w WHERE w.entry_id = o.id AND w.done = 0)
                ORDER BY created_at LIMIT 1
                """, arguments: [Self.now()]).map { row in
                DueEntry(id: row["id"], method: row["method"], request: row["request"], createdAt: row["created_at"],
                         attempts: row["attempts"], quiet: row["quiet"], isUpload: (row["kind"] as String) == "upload")
            }
        }
    }

    private func send(_ entry: DueEntry) async {
        let (status, message) = entry.isUpload
            ? await advanceUpload(entry.id)
            : await call(entry.method, entry.request, requestId: entry.id)

        let now = Self.now()
        let attempts = entry.attempts + 1
        let forgotten = (try? await database.write { db -> [String] in
            if status == GrpcWebTransport.ok {
                return entry.isUpload ? [] : try Self.remove(entry.id, db)
            } else if Self.retriedStatuses.contains(status) && now - entry.createdAt < Self.giveUpAfterMilliseconds {
                let backoff = min(Self.maximumBackoffSeconds, 5 * pow(2, Double(min(attempts, 10)))) * Double.random(in: 0.8...1.2)
                try db.execute(sql: "UPDATE outbox SET attempts = ?, next_attempt_at = ?, last_error = ? WHERE id = ?",
                               arguments: [attempts, now + Int64(backoff * 1000), message, entry.id])
            } else if entry.quiet {
                try db.execute(sql: "DELETE FROM outbox WHERE id = ?", arguments: [entry.id])
            } else {
                let reason = message.isEmpty ? "status \(status)" : message
                try db.execute(sql: "UPDATE outbox SET attempts = ?, failed = 1, last_error = ? WHERE id = ?",
                               arguments: [attempts, reason, entry.id])
                if entry.isUpload {
                    try db.execute(sql: """
                        UPDATE outbox SET failed = 1, last_error = ?
                        WHERE id IN (SELECT entry_id FROM outbox_waits WHERE upload_id = ? AND done = 0)
                        """, arguments: [reason, entry.id])
                }
            }
            return []
        }) ?? []
        forgotten.forEach { shelf?.forget($0) }
        if !entry.quiet {
            onChanged()
        }
    }

    private func advanceUpload(_ id: String) async -> (Int, String) {
        for _ in 0..<Self.stepsPerAttempt {
            guard let upload = try? await database.read({ db in try PendingUpload.fetch(id, db) }) else {
                return (GrpcWebTransport.ok, "")
            }
            switch upload.stage {
            case .check:
                let (status, message, body) = await reply(upload.find, requestId: "\(id):check")
                guard status == GrpcWebTransport.ok else { return (status, message) }
                if let mediaId = try? UploadMessages.mediaId(in: body) {
                    finish(id, mediaId: mediaId)
                    return (GrpcWebTransport.ok, "")
                }
                move(id, to: .begin)
            case .begin:
                let (status, message, body) = await reply(upload.begin, requestId: "\(id):begin")
                guard status == GrpcWebTransport.ok else { return (status, message) }
                guard let ticket = try? UploadMessages.ticket(from: body) else { return (GrpcWebTransport.unknown, "The server's upload ticket couldn't be read.") }
                try? await database.write { db in
                    try db.execute(sql: """
                        UPDATE uploads SET stage = ?, upload_id = ?, put_url = ?, put_content_type = ?, put_expires_at = ? WHERE id = ?
                        """, arguments: [UploadStage.put.rawValue, ticket.uploadId, ticket.url.absoluteString,
                                         ticket.contentType.isEmpty ? upload.contentType : ticket.contentType, Self.now() + Self.uploadLinkMargin, id])
                }
            case .put:
                guard let url = upload.putUrl, let shelf, Self.now() < upload.putExpiresAt else {
                    move(id, to: .begin)
                    continue
                }
                let status: Int
                do {
                    status = try await files.put(shelf.file(upload.file), to: url, contentType: upload.putContentType ?? upload.contentType)
                } catch {
                    return (shelf.has(upload.file) ? GrpcWebTransport.unavailable : Self.failedPrecondition,
                            shelf.has(upload.file) ? error.localizedDescription : "\(upload.fileName) is no longer on this phone.")
                }
                switch status {
                case 200..<300: move(id, to: .complete)
                case 403: move(id, to: .begin)
                default: return (GrpcWebTransport.unavailable, "The file couldn't be sent (HTTP \(status)).")
                }
            case .complete:
                let request = UploadMessages.splice(upload.uploadId ?? "", at: UploadMessages.uploadIdPath, into: upload.complete.request)
                let (status, message, body) = await reply(QueuedCall(method: upload.complete.method, request: request), requestId: "\(id):complete")
                switch status {
                case GrpcWebTransport.ok:
                    guard let mediaId = try? UploadMessages.mediaId(in: body) else { return (GrpcWebTransport.unknown, "The server didn't say which file it kept.") }
                    finish(id, mediaId: mediaId)
                    return (GrpcWebTransport.ok, "")
                case Self.notFound: move(id, to: .check)
                case Self.failedPrecondition: move(id, to: .put)
                default: return (status, message)
                }
            case .done:
                return (GrpcWebTransport.ok, "")
            }
        }
        return (GrpcWebTransport.unavailable, "The upload didn't finish. It will be tried again.")
    }

    private func finish(_ id: String, mediaId: String) {
        let forgotten = (try? database.write { db -> [String] in
            try db.execute(sql: "UPDATE uploads SET stage = ?, media_id = ? WHERE id = ?", arguments: [UploadStage.done.rawValue, mediaId, id])
            try db.execute(sql: "DELETE FROM outbox WHERE id = ?", arguments: [id])
            for wait in try Row.fetchAll(db, sql: "SELECT entry_id, path FROM outbox_waits WHERE upload_id = ? AND done = 0", arguments: [id]) {
                let entryId: String = wait["entry_id"]
                guard let path = UploadMessages.path(wait["path"]),
                      let request = try Data.fetchOne(db, sql: "SELECT request FROM outbox WHERE id = ?", arguments: [entryId]) else { continue }
                try db.execute(sql: "UPDATE outbox SET request = ?, failed = 0, last_error = NULL WHERE id = ?",
                               arguments: [UploadMessages.splice(mediaId, at: path, into: request), entryId])
            }
            try db.execute(sql: "UPDATE outbox_waits SET done = 1 WHERE upload_id = ?", arguments: [id])
            return try Self.releaseUnused([id], db)
        }) ?? []
        forgotten.forEach { shelf?.forget($0) }
    }

    private func move(_ id: String, to stage: UploadStage) {
        try? database.write { db in try db.execute(sql: "UPDATE uploads SET stage = ? WHERE id = ?", arguments: [stage.rawValue, id]) }
    }

    private func call(_ method: String, _ request: Data, requestId: String) async -> (Int, String) {
        let (status, message, _) = await reply(QueuedCall(method: method, request: request), requestId: requestId)
        return (status, message)
    }

    private func reply(_ queued: QueuedCall, requestId: String) async -> (Int, String, Data) {
        do {
            let result = try await transport.unary(method: queued.method, message: queued.request, headers: [Self.requestIdHeader: requestId])
            return (result.status, result.message, result.body)
        } catch let error as GrpcError {
            return (error.status, error.message, Data())
        } catch {
            return (GrpcWebTransport.unavailable, error.localizedDescription, Data())
        }
    }

    private static func uploadFor(_ upload: QueuedUpload, _ db: Database, _ now: Int64) throws -> (id: String, mediaId: String?) {
        if let existing = try Row.fetchOne(db, sql: """
            SELECT u.id, u.media_id FROM uploads u LEFT JOIN outbox o ON o.id = u.id
            WHERE u.file = ? AND (u.stage = 'done' OR (o.id IS NOT NULL AND o.failed = 0)) LIMIT 1
            """, arguments: [upload.file]) {
            return (existing["id"], existing["media_id"])
        }
        let id = UUID().uuidString
        try db.execute(sql: "INSERT INTO outbox (id, method, request, created_at, next_attempt_at, kind) VALUES (?, ?, ?, ?, ?, 'upload')",
                       arguments: [id, upload.complete.method, Data(), now, now])
        try db.execute(sql: """
            INSERT INTO uploads (id, file, file_name, content_type, bytes, find_method, find_request, begin_method, begin_request,
                                 complete_method, complete_request, stage)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """, arguments: [id, upload.file, upload.fileName, upload.contentType, upload.bytes,
                             upload.find.method, upload.find.request, upload.begin.method, upload.begin.request,
                             upload.complete.method, upload.complete.request, UploadStage.check.rawValue])
        return (id, nil)
    }

    private static func remove(_ id: String, _ db: Database) throws -> [String] {
        let uploads = try String.fetchAll(db, sql: "SELECT upload_id FROM outbox_waits WHERE entry_id = ?", arguments: [id])
        try db.execute(sql: "DELETE FROM outbox_waits WHERE entry_id = ?", arguments: [id])
        try db.execute(sql: "DELETE FROM outbox WHERE id = ?", arguments: [id])
        let ownFile = try String.fetchOne(db, sql: "SELECT file FROM uploads WHERE id = ?", arguments: [id])
        try db.execute(sql: "DELETE FROM uploads WHERE id = ?", arguments: [id])
        let released = try releaseUnused(uploads, db)
        guard let ownFile, try !isKept(ownFile, db) else { return released }
        return released + [ownFile]
    }

    private static func releaseUnused(_ uploads: [String], _ db: Database) throws -> [String] {
        var forgotten: [String] = []
        for upload in Set(uploads) {
            let waited = try Bool.fetchOne(db, sql: "SELECT EXISTS (SELECT 1 FROM outbox_waits WHERE upload_id = ?)", arguments: [upload]) ?? false
            guard !waited, let file = try String.fetchOne(db, sql: "SELECT file FROM uploads WHERE id = ?", arguments: [upload]) else { continue }
            try db.execute(sql: "DELETE FROM outbox WHERE id = ?", arguments: [upload])
            try db.execute(sql: "DELETE FROM uploads WHERE id = ?", arguments: [upload])
            if try !isKept(file, db) { forgotten.append(file) }
        }
        return forgotten
    }

    private static func isKept(_ file: String, _ db: Database) throws -> Bool {
        try Bool.fetchOne(db, sql: "SELECT EXISTS (SELECT 1 FROM uploads WHERE file = ?)", arguments: [file]) ?? false
    }

    private static func text(_ path: [Int]) -> String { path.map(String.init).joined(separator: ".") }

    private func scheduleWake() {
        wake?.cancel()
        guard let next = try? database.read({ db in
            try Int64.fetchOne(db, sql: """
                SELECT MIN(next_attempt_at) FROM outbox o WHERE failed = 0
                  AND NOT EXISTS (SELECT 1 FROM outbox_waits w WHERE w.entry_id = o.id AND w.done = 0)
                """)
        }) else { return }
        let delay = max(1, Double(next - Self.now()) / 1000)
        wake = Task {
            try? await Task.sleep(nanoseconds: UInt64(delay * 1_000_000_000))
            guard !Task.isCancelled else { return }
            await drain()
        }
    }

    private static func now() -> Int64 { Int64(Date().timeIntervalSince1970 * 1000) }
}

private enum UploadStage: String {
    case check, begin, put, complete, done
}

private struct PendingUpload {
    let file: String
    let fileName: String
    let contentType: String
    let stage: UploadStage
    let find: QueuedCall
    let begin: QueuedCall
    let complete: QueuedCall
    let uploadId: String?
    let putUrl: URL?
    let putContentType: String?
    let putExpiresAt: Int64

    static func fetch(_ id: String, _ db: Database) throws -> PendingUpload? {
        try Row.fetchOne(db, sql: "SELECT * FROM uploads WHERE id = ?", arguments: [id]).map { row in
            PendingUpload(file: row["file"], fileName: row["file_name"], contentType: row["content_type"],
                          stage: UploadStage(rawValue: row["stage"]) ?? .check,
                          find: QueuedCall(method: row["find_method"], request: row["find_request"]),
                          begin: QueuedCall(method: row["begin_method"], request: row["begin_request"]),
                          complete: QueuedCall(method: row["complete_method"], request: row["complete_request"]),
                          uploadId: row["upload_id"], putUrl: (row["put_url"] as String?).flatMap(URL.init(string:)),
                          putContentType: row["put_content_type"], putExpiresAt: row["put_expires_at"] ?? 0)
        }
    }
}

private struct DueEntry {
    let id: String
    let method: String
    let request: Data
    let createdAt: Int64
    let attempts: Int
    let quiet: Bool
    let isUpload: Bool
}
