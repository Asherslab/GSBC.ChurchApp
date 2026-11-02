import CryptoKit
import XCTest
@testable import DataLayerCore

final class MediaCacheTests: XCTestCase {
    private var directory: URL!

    override func setUpWithError() throws {
        directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
    }

    override func tearDownWithError() throws {
        try? FileManager.default.removeItem(at: directory)
    }

    func testBytesThatDoNotHashToTheirKeyAreNeverCached() throws {
        let cache = try MediaCache(directory: directory)
        let key = Self.key(for: Data("the real photo".utf8))

        XCTAssertThrowsError(try cache.store(key: key, isPrivate: false, downloadedFile: try download(Data("something else".utf8))))
        XCTAssertThrowsError(try cache.store(key: "../../store.sqlite", isPrivate: false, downloadedFile: try download(Data())))
        XCTAssertNil(cache.cached(key: key, isPrivate: false))
    }

    func testTheLeastRecentlyUsedFileGoesFirstWhenTheCacheIsFull() throws {
        let cache = try MediaCache(directory: directory, maximumBytes: 2_500)
        let first = Data(repeating: 1, count: 1_000), second = Data(repeating: 2, count: 1_000), third = Data(repeating: 3, count: 1_000)

        _ = try cache.store(key: Self.key(for: first), isPrivate: false, downloadedFile: try download(first))
        Thread.sleep(forTimeInterval: 0.02)
        _ = try cache.store(key: Self.key(for: second), isPrivate: false, downloadedFile: try download(second))
        Thread.sleep(forTimeInterval: 0.02)
        XCTAssertNotNil(cache.cached(key: Self.key(for: first), isPrivate: false))
        Thread.sleep(forTimeInterval: 0.02)
        _ = try cache.store(key: Self.key(for: third), isPrivate: false, downloadedFile: try download(third))

        XCTAssertNotNil(cache.cached(key: Self.key(for: first), isPrivate: false))
        XCTAssertNil(cache.cached(key: Self.key(for: second), isPrivate: false))
        XCTAssertNotNil(cache.cached(key: Self.key(for: third), isPrivate: false))
        XCTAssertLessThanOrEqual(cache.totalBytes, 2_500)
    }

    func testForgettingPrivateFilesKeepsPublicOnes() throws {
        let cache = try MediaCache(directory: directory)
        let notes = Data("%PDF- notes".utf8), photo = Data("photo".utf8)
        _ = try cache.store(key: Self.key(for: notes, extension: "pdf"), isPrivate: true, downloadedFile: try download(notes))
        _ = try cache.store(key: Self.key(for: photo), isPrivate: false, downloadedFile: try download(photo))

        cache.clearPrivate()

        XCTAssertNil(cache.cached(key: Self.key(for: notes, extension: "pdf"), isPrivate: true))
        XCTAssertNotNil(cache.cached(key: Self.key(for: photo), isPrivate: false))
    }

    private func download(_ data: Data) throws -> URL {
        let file = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString)
        try data.write(to: file)
        return file
    }

    private static func key(for data: Data, extension ext: String = "jpg") -> String {
        SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined() + "." + ext
    }
}
