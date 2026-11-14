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

    func testAFileKeptForANewLinkStaysReadableUntilTheNewOneArrivesAndThenReplacesIt() throws {
        let shelf = DownloadShelf(directory: directory.appendingPathComponent("Kept"), indexDirectory: directory.appendingPathComponent("Index"))
        shelf.replacing("region", url: "https://media.example/v1.pmtiles", title: "Region")
        let old = try shelf.finished("region", movingFrom: temporaryFile(Data(repeating: 1, count: 8)), extension: "pmtiles")

        shelf.replacing("region", url: "https://media.example/v2.pmtiles", title: "Region")
        XCTAssertEqual(shelf.entry("region")?.state, .downloading)
        XCTAssertEqual(try shelf.read("region", offset: 0, length: -1), Data(repeating: 1, count: 8))

        let new = try shelf.finished("region", movingFrom: temporaryFile(Data(repeating: 2, count: 4)), extension: "pmtiles")
        XCTAssertFalse(FileManager.default.fileExists(atPath: old.path))
        XCTAssertEqual(shelf.fileURL("region"), new)
        XCTAssertEqual(shelf.entry("region")?.url, "https://media.example/v2.pmtiles")
        XCTAssertEqual(try shelf.read("region", offset: 0, length: -1), Data(repeating: 2, count: 4))
    }

    func testAReadOfARangeStopsAtTheEndOfTheFile() throws {
        let shelf = DownloadShelf(directory: directory.appendingPathComponent("Kept"), indexDirectory: directory.appendingPathComponent("Index"))
        XCTAssertThrowsError(try shelf.read("map", offset: 0, length: 4))
        shelf.replacing("map", url: "https://media.example/map.pmtiles", title: "Map")
        try shelf.finished("map", movingFrom: temporaryFile(Data(0..<10)), extension: "pmtiles")

        XCTAssertEqual(try shelf.read("map", offset: 2, length: 3), Data([2, 3, 4]))
        XCTAssertEqual(try shelf.read("map", offset: 7, length: 100), Data([7, 8, 9]))
        XCTAssertEqual(try shelf.read("map", offset: 6, length: -1), Data([6, 7, 8, 9]))
        XCTAssertEqual(try shelf.read("map", offset: 20, length: 4), Data())
    }

    private func temporaryFile(_ data: Data) throws -> URL {
        let file = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        try data.write(to: file)
        return file
    }
}
