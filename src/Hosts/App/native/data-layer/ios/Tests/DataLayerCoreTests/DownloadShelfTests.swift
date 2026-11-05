import XCTest
@testable import DataLayerCore

final class DownloadShelfTests: XCTestCase {
    private var directory: URL!

    override func setUpWithError() throws {
        directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    }

    override func tearDownWithError() throws {
        try? FileManager.default.removeItem(at: directory)
    }

    func testADownloadIsKeptOutOfBackupsAndSurvivesTheAppBeingClosed() throws {
        let shelf = DownloadShelf(directory: directory.appendingPathComponent("Documents"), indexDirectory: directory.appendingPathComponent("Support"))
        shelf.started("sermon-a", url: "https://media.example/a.mp3", title: "A sermon")
        let temporary = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        try Data(repeating: 7, count: 2048).write(to: temporary)
        let file = try shelf.finished("sermon-a", movingFrom: temporary, extension: "mp3")

        XCTAssertEqual(try file.resourceValues(forKeys: [.isExcludedFromBackupKey]).isExcludedFromBackup, true)
        XCTAssertEqual(file.lastPathComponent, "A sermon.mp3")
        let reopened = DownloadShelf(directory: directory.appendingPathComponent("Documents"), indexDirectory: directory.appendingPathComponent("Support"))
        XCTAssertEqual(reopened.fileURL("sermon-a"), file)
        XCTAssertEqual(reopened.bytesOnDisk, 2048)
        XCTAssertEqual(try FileManager.default.contentsOfDirectory(atPath: directory.appendingPathComponent("Documents").path), ["A sermon.mp3"])
    }

    func testAFileDeletedOutsideTheAppIsNoLongerListedAsDownloaded() throws {
        let shelf = DownloadShelf(directory: directory.appendingPathComponent("Documents"), indexDirectory: directory.appendingPathComponent("Support"))
        shelf.started("sermon-c", url: "https://media.example/c.mp3", title: "Psalm 23: The Lord / My Shepherd")
        let temporary = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        try Data(repeating: 2, count: 10).write(to: temporary)
        let file = try shelf.finished("sermon-c", movingFrom: temporary, extension: "mp3")
        XCTAssertEqual(file.lastPathComponent, "Psalm 23 The Lord My Shepherd.mp3")

        try FileManager.default.removeItem(at: file)
        XCTAssertTrue(shelf.all.isEmpty)
        XCTAssertNil(shelf.fileURL("sermon-c"))
    }

    func testRemovingADownloadDeletesItsFileAndAnUnfinishedOneHasNoFile() throws {
        let shelf = DownloadShelf(directory: directory.appendingPathComponent("Documents"), indexDirectory: directory.appendingPathComponent("Support"))
        shelf.started("sermon-b", url: "https://media.example/b.mp3", title: "B")
        XCTAssertNil(shelf.fileURL("sermon-b"))
        let temporary = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        try Data(repeating: 1, count: 10).write(to: temporary)
        let file = try shelf.finished("sermon-b", movingFrom: temporary, extension: "mp3")

        shelf.remove("sermon-b")
        XCTAssertFalse(FileManager.default.fileExists(atPath: file.path))
        XCTAssertNil(shelf.entry("sermon-b"))
    }
}
