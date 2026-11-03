import GRDB
import XCTest
@testable import DataLayerCore

final class OutboxTests: XCTestCase {
    private var directory: URL!

    override func setUpWithError() throws {
        directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    }

    override func tearDownWithError() throws {
        try? FileManager.default.removeItem(at: directory)
    }

    func testQuietEntriesNeverShowAndVanishWhenTheyGiveUp() async throws {
        let store = try FeedStore(directory: directory)
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [UnreachableGateway.self]
        let transport = GrpcWebTransport(gateway: URL(string: "https://gateway.invalid")!, session: URLSession(configuration: configuration))
        let outbox = Outbox(database: store.database, transport: transport, onChanged: {})

        _ = try await outbox.enqueue(method: "/gsbc.prayer.Prayer/Submit", request: Data())
        _ = try await outbox.enqueue(method: "/gsbc.analytics.Diagnostics/Report", request: Data(), quiet: true)
        let listed = try await outbox.entries().map(\.method)
        XCTAssertEqual(listed, ["/gsbc.prayer.Prayer/Submit"])

        try await waitUntilEveryEntryWasTried(store.database)
        let fourDaysAgo = Int64(Date().addingTimeInterval(-4 * 24 * 60 * 60).timeIntervalSince1970 * 1000)
        try await store.database.write { db in
            try db.execute(sql: "UPDATE outbox SET created_at = ?, next_attempt_at = 0", arguments: [fourDaysAgo])
        }
        try await drainUntilOnlyOneEntryIsLeft(outbox, store.database)

        let remaining: [Row] = try await store.database.read { db in try Row.fetchAll(db, sql: "SELECT quiet, failed FROM outbox") }
        XCTAssertEqual(remaining.count, 1)
        XCTAssertEqual(remaining.first?["quiet"], false)
        XCTAssertEqual(remaining.first?["failed"], true)
    }

    private func waitUntilEveryEntryWasTried(_ database: DatabaseQueue) async throws {
        for _ in 0..<600 {
            let untried: Int = try await database.read { db in try Int.fetchOne(db, sql: "SELECT COUNT(*) FROM outbox WHERE attempts = 0") ?? 0 }
            if untried == 0 { return }
            try await Task.sleep(nanoseconds: 50_000_000)
        }
        XCTFail("The outbox never tried to send")
    }

    private func drainUntilOnlyOneEntryIsLeft(_ outbox: Outbox, _ database: DatabaseQueue) async throws {
        for _ in 0..<600 {
            await outbox.drain()
            let count: Int = try await database.read { db in try Int.fetchOne(db, sql: "SELECT COUNT(*) FROM outbox") ?? 0 }
            if count <= 1 { return }
            try await Task.sleep(nanoseconds: 50_000_000)
        }
    }
}

private final class UnreachableGateway: URLProtocol {
    override class func canInit(with request: URLRequest) -> Bool { true }
    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        client?.urlProtocol(self, didFailWithError: URLError(.cannotConnectToHost))
    }

    override func stopLoading() {}
}
