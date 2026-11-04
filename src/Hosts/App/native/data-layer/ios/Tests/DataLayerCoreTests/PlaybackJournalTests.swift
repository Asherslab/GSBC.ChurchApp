import XCTest
@testable import DataLayerCore

final class PlaybackJournalTests: XCTestCase {
    private var directory: URL!

    override func setUpWithError() throws {
        directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    }

    override func tearDownWithError() throws {
        try? FileManager.default.removeItem(at: directory)
    }

    func testWhereYouWereSurvivesTheAppBeingKilled() {
        let journal = PlaybackJournal(directory: directory)
        journal.heard("sermon-a", position: 1834, duration: 3120)
        journal.mark("sermon-b", "watched", at: 1_700_000_000_000)

        let reopened = PlaybackJournal(directory: directory)
        XCTAssertEqual(reopened.record("sermon-a")?.position, 1834)
        XCTAssertEqual(reopened.record("sermon-a")?.duration, 3120)
        XCTAssertEqual(reopened.record("sermon-b")?.marks["watched"], 1_700_000_000_000)
    }

    func testOnlyWhatChangedSinceTheLastSendIsSentAgain() {
        let journal = PlaybackJournal(directory: directory)
        journal.heard("sermon-a", position: 60, duration: 3000)
        journal.heard("sermon-b", position: 90, duration: 3000)
        let sent = journal.since(0)
        XCTAssertEqual(sent.records.map(\.id), ["sermon-a", "sermon-b"])

        journal.heard("sermon-a", position: 120, duration: 3000)
        let next = journal.since(sent.latest)
        XCTAssertEqual(next.records.map(\.id), ["sermon-a"])
        XCTAssertEqual(next.records.first?.position, 120)
        XCTAssertEqual(journal.since(0).records.count, 2)
    }
}
