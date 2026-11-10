import GRDB
import XCTest
@testable import DataLayerCore

final class UploadOutboxTests: XCTestCase {
    private static let find = "/gsbc.media.Media/FindByHash"
    private static let begin = "/gsbc.media.Media/BeginUpload"
    private static let complete = "/gsbc.media.Media/CompleteUpload"
    private static let save = "/gsbc.ministries.MinistryLeading/SaveFile"
    private static let mediaIdPath = [2, 6]

    private var directory: URL!
    private var store: FeedStore!
    private var shelf: UploadShelf!
    private var files: FakeBucket!
    private var outbox: Outbox!

    override func setUpWithError() throws {
        directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        store = try FeedStore(directory: directory)
        shelf = try UploadShelf(directory: directory.appendingPathComponent("Uploads"))
        files = FakeBucket()
        FakeMediaGateway.reset()
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [FakeMediaGateway.self]
        let transport = GrpcWebTransport(gateway: URL(string: "https://gateway.invalid")!, session: URLSession(configuration: configuration))
        outbox = Outbox(database: store.database, transport: transport, shelf: shelf, files: files, onChanged: {})
    }

    override func tearDownWithError() throws {
        try? FileManager.default.removeItem(at: directory)
    }

    func testAnUploadQueuedOfflineIsSentOnceWhenOnlineAndTheSaveGetsItsMediaId() async throws {
        FakeMediaGateway.online = false
        await files.go(online: false)
        let staged = try stage("photo")

        _ = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(staged)])
        try await settle { try await self.everyEntryWasTried() }

        let waiting = try await outbox.entries()
        XCTAssertEqual(waiting.count, 2)
        XCTAssertEqual(waiting.first { $0.upload != nil }?.upload?.fileName, "photo.jpg")
        XCTAssertTrue(waiting.first { $0.upload == nil }!.waitsForUpload)
        XCTAssertFalse(FakeMediaGateway.calls.contains { $0.path == Self.save })

        FakeMediaGateway.online = true
        await files.go(online: true)
        await outbox.sendWaitingNow()
        try await settle { try await self.outbox.entries().isEmpty }

        XCTAssertEqual(FakeMediaGateway.calls.map(\.path), [Self.find, Self.begin, Self.complete, Self.save])
        let puts = await files.puts
        XCTAssertEqual(puts, ["https://bucket.invalid/uploads/upload-1"])
        let sent = try XCTUnwrap(FakeMediaGateway.calls.last?.body)
        XCTAssertEqual(try Self.mediaId(in: sent), FakeMediaGateway.newMediaId)
        XCTAssertFalse(shelf.has(staged.token))
    }

    func testAFileTheLibraryAlreadyHasIsNotUploadedAgain() async throws {
        FakeMediaGateway.existing = "media-already-there"
        let staged = try stage("again")

        _ = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(staged)])
        try await settle { try await self.outbox.entries().isEmpty }

        XCTAssertEqual(FakeMediaGateway.calls.map(\.path), [Self.find, Self.save])
        let puts = await files.puts
        XCTAssertTrue(puts.isEmpty)
        XCTAssertEqual(try Self.mediaId(in: FakeMediaGateway.calls.last!.body), "media-already-there")
        XCTAssertFalse(shelf.has(staged.token))
    }

    func testDiscardingTheUploadDropsTheSaveThatWaitsForItAndTheFile() async throws {
        FakeMediaGateway.online = false
        let staged = try stage("discarded")
        _ = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(staged)])
        let queued = try await outbox.entries()
        let uploadId = try XCTUnwrap(queued.first { $0.upload != nil }?.id)

        try await outbox.discard(id: uploadId)

        let left = try await outbox.entries()
        XCTAssertTrue(left.isEmpty)
        XCTAssertFalse(shelf.has(staged.token))
    }

    func testDiscardingTheSaveDropsTheUploadItWaitedFor() async throws {
        FakeMediaGateway.online = false
        let staged = try stage("record-deleted")
        let saveId = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(staged)])

        try await outbox.discard(id: saveId)

        let left = try await outbox.entries()
        XCTAssertTrue(left.isEmpty)
        XCTAssertFalse(shelf.has(staged.token))
        XCTAssertTrue(try Outbox.keptFiles(in: store.database).isEmpty)
    }

    func testTwoSavesUsingOneFileShareOneUpload() async throws {
        FakeMediaGateway.online = false
        let staged = try stage("shared")
        _ = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(staged)])
        _ = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(staged)])

        FakeMediaGateway.online = true
        await outbox.sendWaitingNow()
        try await settle { try await self.outbox.entries().isEmpty }

        XCTAssertEqual(FakeMediaGateway.calls.filter { $0.path == Self.complete }.count, 1)
        XCTAssertEqual(try FakeMediaGateway.calls.filter { $0.path == Self.save }.map { try Self.mediaId(in: $0.body) },
                       [FakeMediaGateway.newMediaId, FakeMediaGateway.newMediaId])
        XCTAssertFalse(shelf.has(staged.token))
    }

    func testAnUploadWhoseCompletionReplyWasLostIsFoundRatherThanSentAgain() async throws {
        FakeMediaGateway.loseFirstCompletion = true
        let staged = try stage("lost-reply")

        _ = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(staged)])
        try await settle { try await self.outbox.entries().isEmpty }

        XCTAssertEqual(FakeMediaGateway.calls.map(\.path), [Self.find, Self.begin, Self.complete, Self.complete, Self.find, Self.save])
        let puts = await files.puts
        XCTAssertEqual(puts.count, 1)
        XCTAssertEqual(try Self.mediaId(in: FakeMediaGateway.calls.last!.body), FakeMediaGateway.newMediaId)
    }

    func testAnUploadTheServerRefusesFailsItsSaveAndRetryingTheSaveTriesTheUploadAgain() async throws {
        FakeMediaGateway.refuseBegin = true
        let staged = try stage("refused")
        let saveId = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(staged)])
        try await settle { try await self.outbox.entries().allSatisfy(\.failed) }

        let failed = try await outbox.entries()
        let failedSave = try XCTUnwrap(failed.first { $0.id == saveId })
        XCTAssertEqual(failedSave.lastError, "Not that kind of file.")
        XCTAssertTrue(shelf.has(staged.token))

        FakeMediaGateway.refuseBegin = false
        try await outbox.retry(id: saveId)
        try await settle { try await self.outbox.entries().isEmpty }
        XCTAssertEqual(FakeMediaGateway.calls.last?.path, Self.save)
        XCTAssertFalse(shelf.has(staged.token))
    }

    func testFilesNothingQueuedAreClearedFromTheShelf() async throws {
        FakeMediaGateway.online = false
        let queued = try stage("queued")
        let abandoned = try stage("abandoned")
        _ = try await outbox.enqueue(method: Self.save, request: Self.fileSave(), uploads: [upload(queued)])

        shelf.sweep(keeping: try Outbox.keptFiles(in: store.database))

        XCTAssertTrue(shelf.has(queued.token))
        XCTAssertFalse(shelf.has(abandoned.token))
    }

    func testSplicingSetsAFieldInsideTheMessageWithoutDisturbingTheRest() throws {
        var file = ProtoWriter()
        file.string(3, "Helpers' guide")
        var save = ProtoWriter()
        save.string(1, "ministry-1")
        save.bytes(2, file.data)

        let spliced = UploadMessages.splice("media-9", at: [2, 6], into: save.data)

        XCTAssertEqual(try Self.mediaId(in: spliced), "media-9")
        XCTAssertTrue(spliced.starts(with: save.data))
    }

    private func stage(_ name: String) throws -> StagedFile {
        let token = try shelf.begin(extension: "jpg")
        try shelf.append(token, Data("bytes of \(name)".utf8))
        return try shelf.finish(token)
    }

    private func upload(_ staged: StagedFile) -> QueuedUpload {
        var find = ProtoWriter()
        find.string(1, staged.sha256)
        find.string(2, "image/jpeg")
        var begin = ProtoWriter()
        begin.string(1, "photo.jpg")
        begin.string(2, "image/jpeg")
        return QueuedUpload(file: staged.token, fileName: "photo.jpg", contentType: "image/jpeg", bytes: staged.bytes,
                            find: QueuedCall(method: Self.find, request: find.data),
                            begin: QueuedCall(method: Self.begin, request: begin.data),
                            complete: QueuedCall(method: Self.complete, request: Data()),
                            paths: [Self.mediaIdPath])
    }

    private func everyEntryWasTried() async throws -> Bool {
        try await store.database.read { db in
            try Int.fetchOne(db, sql: """
                SELECT COUNT(*) FROM outbox o WHERE attempts = 0
                  AND NOT EXISTS (SELECT 1 FROM outbox_waits w WHERE w.entry_id = o.id AND w.done = 0)
                """) ?? 0
        } == 0
    }

    private func settle(_ done: @escaping () async throws -> Bool) async throws {
        for _ in 0..<600 {
            if try await done() { return }
            await outbox.sendWaitingNow()
            try await Task.sleep(nanoseconds: 20_000_000)
        }
        XCTFail("The outbox never settled")
    }

    private static func fileSave() -> Data {
        var file = ProtoWriter()
        file.string(3, "Helpers' guide")
        var save = ProtoWriter()
        save.string(1, "ministry-1")
        save.bytes(2, file.data)
        return save.data
    }

    private static func mediaId(in save: Data) throws -> String? {
        var outer = ProtoReader(save)
        var found: String?
        while !outer.isAtEnd {
            let (field, wireType) = try outer.nextField()
            guard field == 2 else { try outer.skip(wireType: wireType); continue }
            var inner = ProtoReader(try outer.bytes())
            while !inner.isAtEnd {
                let (innerField, innerType) = try inner.nextField()
                if innerField == 6 { found = try inner.string() } else { try inner.skip(wireType: innerType) }
            }
        }
        return found
    }
}

private actor FakeBucket: FileSender {
    private(set) var puts: [String] = []
    private var online = true

    func go(online: Bool) {
        self.online = online
    }

    nonisolated func put(_ file: URL, to url: URL, contentType: String) async throws -> Int {
        try await record(url, FileManager.default.fileExists(atPath: file.path))
    }

    private func record(_ url: URL, _ fileExists: Bool) throws -> Int {
        guard online else { throw URLError(.notConnectedToInternet) }
        guard fileExists else { throw URLError(.fileDoesNotExist) }
        puts.append(url.absoluteString)
        return 200
    }
}

private final class FakeMediaGateway: URLProtocol {
    static let newMediaId = "media-new"
    private static let lock = NSLock()
    nonisolated(unsafe) private static var state = State()

    private struct State {
        var online = true
        var existing: String?
        var loseFirstCompletion = false
        var refuseBegin = false
        var completed: String?
        var calls: [(path: String, body: Data)] = []
    }

    static var online: Bool {
        get { lock.withLock { state.online } }
        set { lock.withLock { state.online = newValue } }
    }

    static var existing: String? {
        get { lock.withLock { state.existing } }
        set { lock.withLock { state.existing = newValue } }
    }

    static var loseFirstCompletion: Bool {
        get { lock.withLock { state.loseFirstCompletion } }
        set { lock.withLock { state.loseFirstCompletion = newValue } }
    }

    static var refuseBegin: Bool {
        get { lock.withLock { state.refuseBegin } }
        set { lock.withLock { state.refuseBegin = newValue } }
    }

    static var calls: [(path: String, body: Data)] { lock.withLock { state.calls } }

    static func reset() {
        lock.withLock { state = State() }
    }

    override class func canInit(with request: URLRequest) -> Bool { true }
    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        guard Self.online else {
            client?.urlProtocol(self, didFailWithError: URLError(.cannotConnectToHost))
            return
        }
        let path = request.url!.path
        let body = Self.unframe(Self.bodyOf(request))
        var status = "0"
        var message = ""
        var reply = Data()
        Self.lock.withLock {
            Self.state.calls.append((path, body))
            switch path {
            case "/gsbc.media.Media/FindByHash":
                if let match = Self.state.existing ?? Self.state.completed { reply = Self.matches(match) }
            case "/gsbc.media.Media/BeginUpload":
                if Self.state.refuseBegin {
                    (status, message) = ("3", "Not that kind of file.")
                } else {
                    var ticket = ProtoWriter()
                    ticket.string(1, "upload-1")
                    ticket.string(2, "https://bucket.invalid/uploads/upload-1")
                    ticket.string(3, "image/jpeg")
                    reply = ticket.data
                }
            case "/gsbc.media.Media/CompleteUpload":
                if Self.state.completed != nil {
                    (status, message) = ("5", "That upload has expired. Start again.")
                } else {
                    Self.state.completed = Self.newMediaId
                    reply = Self.matches(Self.newMediaId)
                    if Self.state.loseFirstCompletion { (status, reply) = ("14", Data()) }
                }
            default:
                break
            }
        }
        var headers = ["Content-Type": "application/grpc-web+proto", "grpc-status": status]
        if !message.isEmpty { headers["grpc-message"] = message }
        let response = HTTPURLResponse(url: request.url!, statusCode: 200, httpVersion: "HTTP/1.1", headerFields: headers)!
        client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
        client?.urlProtocol(self, didLoad: Self.frame(reply))
        client?.urlProtocolDidFinishLoading(self)
    }

    override func stopLoading() {}

    private static func matches(_ mediaId: String) -> Data {
        var media = ProtoWriter()
        media.string(1, mediaId)
        var item = ProtoWriter()
        item.bytes(1, media.data)
        var reply = ProtoWriter()
        reply.bytes(1, item.data)
        return reply.data
    }

    private static func bodyOf(_ request: URLRequest) -> Data {
        if let body = request.httpBody { return body }
        guard let stream = request.httpBodyStream else { return Data() }
        stream.open()
        defer { stream.close() }
        var data = Data()
        var buffer = [UInt8](repeating: 0, count: 4096)
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: buffer.count)
            guard read > 0 else { break }
            data.append(buffer, count: read)
        }
        return data
    }

    private static func unframe(_ framed: Data) -> Data {
        framed.count >= 5 ? Data(framed.dropFirst(5)) : Data()
    }

    private static func frame(_ payload: Data) -> Data {
        var framed = Data([0])
        withUnsafeBytes(of: UInt32(payload.count).bigEndian) { framed.append(contentsOf: $0) }
        return framed + payload
    }
}
