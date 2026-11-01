import XCTest
@testable import DataLayerCore

final class SyncWireFormatTests: XCTestCase {
    private static let watchEventHex =
        "0a4e0a066576656e7473102a22340a026531100322030102032a0a323032362d31312d30383206089882c1d7063a0b08b0acc1d7061080e59a77420673756e646179220a0a02653010041801220028010a0b0a076e6f74696365731801"

    func testDecodesTheWatchEventTheServerEncodes() throws {
        let event = try SyncMessages.watchEvent(Data(hex: Self.watchEventHex))

        XCTAssertFalse(event.isHeartbeat)
        XCTAssertEqual(event.batches.map(\.feed), ["events", "notices"])

        let events = event.batches[0]
        XCTAssertEqual(events.cursor, 42)
        XCTAssertTrue(events.hasMore)
        XCTAssertFalse(events.reset)

        let upsert = events.changes[0]
        XCTAssertEqual(upsert.id, "e1")
        XCTAssertEqual(upsert.version, 3)
        XCTAssertEqual(upsert.payload, Data([1, 2, 3]))
        XCTAssertEqual(upsert.sortKey, "2026-11-08")
        XCTAssertEqual(upsert.startsAt, 1_794_130_200_000)
        XCTAssertEqual(upsert.endsAt, 1_794_135_600_250)
        XCTAssertEqual(upsert.groupKey, "sunday")

        let delete = events.changes[1]
        XCTAssertEqual(delete.id, "e0")
        XCTAssertEqual(delete.version, 4)
        XCTAssertTrue(delete.deleted)

        XCTAssertTrue(event.batches[1].reset)
    }

    func testEncodesCursorsTheWayTheServerReadsThem() {
        let request = SyncMessages.cursorsRequest(["dev.echo": 0])

        XCTAssertEqual(request, Data(hex: "0a0a0a086465762e6563686f"))
    }
}

extension Data {
    init(hex: String) {
        var bytes = [UInt8]()
        var index = hex.startIndex
        while index < hex.endIndex {
            let next = hex.index(index, offsetBy: 2)
            bytes.append(UInt8(hex[index..<next], radix: 16)!)
            index = next
        }
        self.init(bytes)
    }
}
