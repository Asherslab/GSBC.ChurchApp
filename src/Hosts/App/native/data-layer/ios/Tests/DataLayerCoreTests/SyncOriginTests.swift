import XCTest
@testable import DataLayerCore

final class SyncOriginTests: XCTestCase {
    private var directory: URL!

    override func setUpWithError() throws {
        directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    }

    override func tearDownWithError() throws {
        try? FileManager.default.removeItem(at: directory)
    }

    func testAPhoneThatHasNeverHeardAnOriginAsksAsUnknownSoItsOldRowsAreReplaced() throws {
        let store = try FeedStore(directory: directory)
        try store.register(feeds: ["sermons.series"])
        _ = try store.apply([FeedBatch(feed: "sermons.series", cursor: 9, changes: [series("anchored-a")])])

        XCTAssertNil(try store.origin())
        XCTAssertEqual(SyncMessages.cursorsRequest(try store.cursors(for: ["sermons.series"]), origin: try store.origin()),
                       SyncMessages.cursorsRequest(["sermons.series": 9], origin: "unknown"))
    }

    func testResetsFromAnotherOriginDropEveryStaleRowAndKeepTheNewOrigin() throws {
        let store = try FeedStore(directory: directory)
        try store.register(feeds: ["sermons.series", "events"])
        _ = try store.apply([
            FeedBatch(feed: "sermons.series", cursor: 9, changes: [series("anchored-a"), series("ten-a")]),
            FeedBatch(feed: "events", cursor: 4, changes: [series("easter")]),
        ], origin: "database-a")

        _ = try store.apply([
            FeedBatch(feed: "sermons.series", reset: true, hasMore: true),
            FeedBatch(feed: "events", reset: true, hasMore: true),
        ], origin: "database-b")
        _ = try store.apply([FeedBatch(feed: "sermons.series", cursor: 3, changes: [series("anchored-b")])], origin: "database-b")

        XCTAssertEqual(try store.origin(), "database-b")
        XCTAssertEqual(try store.page(feed: "sermons.series", order: .sortKey, descending: false, limit: 10, offset: 0, groupKey: nil).map(\.id),
                       ["anchored-b"])
        XCTAssertEqual(try store.page(feed: "events", order: .sortKey, descending: false, limit: 10, offset: 0, groupKey: nil).count, 0)
        XCTAssertEqual(try store.cursors(for: ["sermons.series", "events"]), ["sermons.series": 3, "events": 0])
    }

    func testTheSameOriginKeepsEverything() throws {
        let store = try FeedStore(directory: directory)
        try store.register(feeds: ["sermons.series"])
        _ = try store.apply([FeedBatch(feed: "sermons.series", cursor: 9, changes: [series("anchored-a")])], origin: "database-a")

        XCTAssertEqual(try store.apply([], origin: "database-a"), [])

        XCTAssertEqual(try store.origin(), "database-a")
        XCTAssertEqual(try store.cursors(for: ["sermons.series"]), ["sermons.series": 9])
        XCTAssertEqual(try store.page(feed: "sermons.series", order: .sortKey, descending: false, limit: 10, offset: 0, groupKey: nil).count, 1)
    }

    private func series(_ id: String) -> FeedChange {
        FeedChange(id: id, version: 1, payload: Data(id.utf8), sortKey: id)
    }
}
