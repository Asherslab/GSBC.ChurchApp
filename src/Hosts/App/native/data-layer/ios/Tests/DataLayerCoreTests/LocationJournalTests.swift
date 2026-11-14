import XCTest
@testable import DataLayerCore

final class LocationJournalTests: XCTestCase {
    private var directory: URL!

    override func setUpWithError() throws {
        directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    }

    override func tearDownWithError() throws {
        try? FileManager.default.removeItem(at: directory)
    }

    func testNumbersKeepRisingAfterTheAppIsClosedEvenOnceEverythingIsCleared() {
        let journal = LocationJournal(directory: directory)
        walk(journal, fixes: 3)
        journal.clear(upTo: 3)

        let reopened = LocationJournal(directory: directory)
        XCTAssertEqual(reopened.latest, 3)
        XCTAssertEqual(reopened.append(at: 10, lat: 1, lon: 1, accuracy: 5, heading: -1, speed: -1).seq, 4)
        XCTAssertEqual(LocationJournal(directory: directory).since(0).fixes.map(\.seq), [4])
    }

    func testSinceReturnsOnlyNewerFixesAndClearDropsOnlyThoseUpToTheMark() {
        let journal = LocationJournal(directory: directory)
        walk(journal, fixes: 5)

        XCTAssertEqual(journal.since(2).fixes.map(\.seq), [3, 4, 5])
        XCTAssertEqual(journal.since(2).latest, 5)
        journal.clear(upTo: 3)
        XCTAssertEqual(LocationJournal(directory: directory).since(0).fixes.map(\.seq), [4, 5])
    }

    func testTheOldestFixesGoOnceTheJournalIsFull() {
        let journal = LocationJournal(directory: directory, cap: 10)
        walk(journal, fixes: 25)

        XCTAssertEqual(LocationJournal(directory: directory, cap: 10).since(0).fixes.map(\.seq), Array(16...25))
    }

    func testALastLineCutShortByTheAppBeingKilledIsIgnoredAndLaterFixesStillSave() throws {
        let journal = LocationJournal(directory: directory)
        walk(journal, fixes: 2)
        let file = directory.appendingPathComponent("fixes.jsonl")
        let handle = try FileHandle(forWritingTo: file)
        try handle.seekToEnd()
        try handle.write(contentsOf: Data("{\"seq\":3,\"at\":3,\"lat\":-33.8".utf8))
        try handle.close()

        let reopened = LocationJournal(directory: directory)
        XCTAssertEqual(reopened.since(0).fixes.map(\.seq), [1, 2])
        reopened.append(at: 4, lat: 2, lon: 2, accuracy: 4, heading: 90, speed: 1.2)
        XCTAssertEqual(LocationJournal(directory: directory).since(0).fixes.map(\.seq), [1, 2, 3])
    }

    private func walk(_ journal: LocationJournal, fixes: Int) {
        for index in 0..<fixes {
            journal.append(at: Int64(index), lat: -33.8 + Double(index) / 10_000, lon: 151.2, accuracy: 5, heading: -1, speed: -1)
        }
    }
}
