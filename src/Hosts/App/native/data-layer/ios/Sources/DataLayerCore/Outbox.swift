import Foundation
import GRDB

public struct OutboxEntry {
    public let id: String
    public let method: String
    public let createdAt: Int64
    public let attempts: Int
    public let lastError: String?
    public let failed: Bool
}

public actor Outbox {
    public static let requestIdHeader = "gsbc-request-id"

    private static let giveUpAfterMilliseconds: Int64 = 3 * 24 * 60 * 60 * 1000
    private static let maximumBackoffSeconds: Double = 60 * 60
    private static let retriedStatuses: Set<Int> = [2, 4, 8, 10, 13, 14]

    private let database: DatabaseQueue
    private let transport: GrpcWebTransport
    private let onChanged: @Sendable () -> Void
    private var draining = false
    private var wake: Task<Void, Never>?

    public init(database: DatabaseQueue, transport: GrpcWebTransport, onChanged: @escaping @Sendable () -> Void) {
        self.database = database
        self.transport = transport
        self.onChanged = onChanged
    }

    public func enqueue(method: String, request: Data) throws -> String {
        let id = UUID().uuidString
        let now = Self.now()
        try database.write { db in
            try db.execute(sql: "INSERT INTO outbox (id, method, request, created_at, next_attempt_at) VALUES (?, ?, ?, ?, ?)",
                           arguments: [id, method, request, now, now])
        }
        onChanged()
        Task { await drain() }
        return id
    }

    public func entries() throws -> [OutboxEntry] {
        try database.read { db in
            try Row.fetchAll(db, sql: "SELECT id, method, created_at, attempts, last_error, failed FROM outbox ORDER BY created_at").map { row in
                OutboxEntry(id: row["id"], method: row["method"], createdAt: row["created_at"],
                            attempts: row["attempts"], lastError: row["last_error"], failed: row["failed"])
            }
        }
    }

    public func retry(id: String) throws {
        try database.write { db in
            try db.execute(sql: "UPDATE outbox SET failed = 0, attempts = 0, created_at = ?, next_attempt_at = ? WHERE id = ?",
                           arguments: [Self.now(), Self.now(), id])
        }
        onChanged()
        Task { await drain() }
    }

    public func discard(id: String) throws {
        try database.write { db in try db.execute(sql: "DELETE FROM outbox WHERE id = ?", arguments: [id]) }
        onChanged()
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

    private func nextDue() throws -> (id: String, method: String, request: Data, createdAt: Int64, attempts: Int)? {
        try database.read { db in
            try Row.fetchOne(db, sql: """
                SELECT id, method, request, created_at, attempts FROM outbox
                WHERE failed = 0 AND next_attempt_at <= ? ORDER BY created_at LIMIT 1
                """, arguments: [Self.now()]).map { row in
                (row["id"], row["method"], row["request"], row["created_at"], row["attempts"])
            }
        }
    }

    private func send(_ entry: (id: String, method: String, request: Data, createdAt: Int64, attempts: Int)) async {
        let status: Int
        let message: String
        do {
            let result = try await transport.unary(method: entry.method, message: entry.request,
                                                   headers: [Self.requestIdHeader: entry.id])
            (status, message) = (result.status, result.message)
        } catch let error as GrpcError {
            (status, message) = (error.status, error.message)
        } catch {
            (status, message) = (GrpcWebTransport.unavailable, error.localizedDescription)
        }

        let now = Self.now()
        let attempts = entry.attempts + 1
        try? await database.write { db in
            if status == GrpcWebTransport.ok {
                try db.execute(sql: "DELETE FROM outbox WHERE id = ?", arguments: [entry.id])
            } else if Self.retriedStatuses.contains(status) && now - entry.createdAt < Self.giveUpAfterMilliseconds {
                let backoff = min(Self.maximumBackoffSeconds, 5 * pow(2, Double(min(attempts, 10)))) * Double.random(in: 0.8...1.2)
                try db.execute(sql: "UPDATE outbox SET attempts = ?, next_attempt_at = ?, last_error = ? WHERE id = ?",
                               arguments: [attempts, now + Int64(backoff * 1000), message, entry.id])
            } else {
                try db.execute(sql: "UPDATE outbox SET attempts = ?, failed = 1, last_error = ? WHERE id = ?",
                               arguments: [attempts, message.isEmpty ? "status \(status)" : message, entry.id])
            }
        }
        onChanged()
    }

    private func scheduleWake() {
        wake?.cancel()
        guard let next = try? database.read({ db in
            try Int64.fetchOne(db, sql: "SELECT MIN(next_attempt_at) FROM outbox WHERE failed = 0")
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
