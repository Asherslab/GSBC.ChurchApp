import XCTest
@testable import DataLayerCore
import GRDB

final class FeedStoreSoakTests: XCTestCase {
    private var directory: URL!

    override func setUpWithError() throws {
        directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    }

    override func tearDownWithError() throws {
        try? FileManager.default.removeItem(at: directory)
    }

    func testMonthsOfChurnLeaveExactlyTheServersStateAndBoundedDisk() throws {
        let store = try FeedStore(directory: directory)
        XCTAssertEqual(try store.database.read { try Int.fetchOne($0, sql: "PRAGMA auto_vacuum") }, 2, "INCREMENTAL")
        var server = SimulatedServer(feeds: ["events", "notices", "sermons"], idsPerFeed: 300)
        try store.register(feeds: server.feeds)

        for day in 1...180 {
            if day == 60 {
                try store.apply([server.resetBatch(feed: "notices")])
            }
            if day == 120 {
                server.drop(feed: "notices")
                try store.register(feeds: server.feeds)
            }
            try store.apply(server.batchesForOneDay(changes: 40))

            for feed in server.feeds {
                XCTAssertEqual(try store.liveVersions(feed: feed), server.live[feed], "day \(day), \(feed)")
            }
        }

        XCTAssertEqual(try store.registeredFeeds(), ["events", "sermons"])
        XCTAssertLessThan(store.fileBytes, 3 * 1024 * 1024)

        for feed in server.feeds {
            try store.apply([FeedBatch(feed: feed, cursor: server.cursor, reset: true)])
        }
        XCTAssertLessThan(store.fileBytes, 512 * 1024, "deleted rows should give their space back")
    }

    func testAFeedOverItsCapKeepsItsNewestRows() throws {
        let store = try FeedStore(directory: directory, limits: FeedLimits(maximumRows: 100, maximumBytes: .max))
        try store.register(feeds: ["events"])

        let changes = (1...250).map { FeedChange(id: "e\($0)", version: 1, payload: Data(count: 10)) }
        for (index, chunk) in stride(from: 0, to: changes.count, by: 50).map({ Array(changes[$0..<min($0 + 50, changes.count)]) }).enumerated() {
            try store.apply([FeedBatch(feed: "events", cursor: Int64(index + 1), changes: chunk)])
        }

        let kept = try store.liveVersions(feed: "events").keys
        XCTAssertEqual(kept.count, 100)
        XCTAssertTrue(kept.allSatisfy { Int($0.dropFirst())! > 150 })
    }
}

private struct SimulatedServer {
    var feeds: [String]
    var live: [String: [String: Int64]] = [:]
    var cursor: Int64 = 0
    private let idsPerFeed: Int
    private var random = SeededRandom(seed: 2026)

    init(feeds: [String], idsPerFeed: Int) {
        self.feeds = feeds
        self.idsPerFeed = idsPerFeed
        for feed in feeds { live[feed] = [:] }
    }

    mutating func batchesForOneDay(changes: Int) -> [FeedBatch] {
        var batches: [String: [FeedChange]] = [:]
        for _ in 0..<changes {
            let feed = feeds[random.next(below: feeds.count)]
            let id = "\(feed)-\(random.next(below: idsPerFeed))"
            let version = (live[feed]?[id] ?? 0) + 1
            if live[feed]?[id] != nil, random.next(below: 3) == 0 {
                live[feed]?[id] = nil
                batches[feed, default: []].append(FeedChange(id: id, version: version, deleted: true))
            } else {
                live[feed]?[id] = version
                batches[feed, default: []].append(FeedChange(id: id, version: version, payload: Data(count: 1024)))
            }
        }
        cursor += 1
        return batches.map { FeedBatch(feed: $0.key, cursor: cursor, changes: $0.value) }
    }

    mutating func resetBatch(feed: String) -> FeedBatch {
        cursor += 1
        let changes = (live[feed] ?? [:]).map { FeedChange(id: $0.key, version: $0.value, payload: Data(count: 1024)) }
        return FeedBatch(feed: feed, cursor: cursor, reset: true, changes: changes)
    }

    mutating func drop(feed: String) {
        feeds.removeAll { $0 == feed }
        live[feed] = nil
    }
}

private struct SeededRandom {
    private var state: UInt64

    init(seed: UInt64) { state = seed }

    mutating func next(below bound: Int) -> Int {
        state = state &* 6364136223846793005 &+ 1442695040888963407
        return Int((state >> 33) % UInt64(bound))
    }
}

private extension FeedStore {
    func liveVersions(feed: String) throws -> [String: Int64] {
        let records = try page(feed: feed, order: .sortKey, descending: false, limit: .max, offset: 0, groupKey: nil)
        return Dictionary(uniqueKeysWithValues: records.map { ($0.id, $0.version) })
    }
}
