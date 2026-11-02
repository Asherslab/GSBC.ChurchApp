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

    func testPrefetchingSkipsLargeFilesStopsAtItsBudgetAndNeverRefetches() async throws {
        let small = Data(repeating: 1, count: 10), large = Data(repeating: 2, count: 30)
        let another = Data(repeating: 3, count: 10), overBudget = Data(repeating: 4, count: 10), lying = Data(repeating: 5, count: 40)
        let files = [small, large, another, overBudget, lying]
        StubbedFiles.serve(Dictionary(uniqueKeysWithValues: files.map { (Self.url(for: $0), $0) }))
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [StubbedFiles.self]
        let cache = try MediaCache(directory: directory, prefetchSession: URLSession(configuration: configuration))
        let items = [
            item(small), item(large), item(lying, declaredBytes: 5), item(another), item(overBudget),
        ]

        let fetched = await cache.prefetch(items, batchLimit: 25, fileLimit: 20)
        let again = await cache.prefetch([item(small)], batchLimit: 25, fileLimit: 20)

        XCTAssertEqual(fetched, 2)
        XCTAssertNotNil(cache.cached(key: Self.key(for: small), isPrivate: false))
        XCTAssertNotNil(cache.cached(key: Self.key(for: another), isPrivate: false))
        XCTAssertNil(cache.cached(key: Self.key(for: large), isPrivate: false))
        XCTAssertNil(cache.cached(key: Self.key(for: lying), isPrivate: false))
        XCTAssertNil(cache.cached(key: Self.key(for: overBudget), isPrivate: false))
        XCTAssertEqual(again, 0)
        XCTAssertEqual(StubbedFiles.requested.filter { $0 == Self.url(for: small) }.count, 1)
    }

    private func item(_ data: Data, declaredBytes: Int? = nil) -> PrefetchItem {
        PrefetchItem(key: Self.key(for: data), url: Self.url(for: data), bytes: declaredBytes ?? data.count, isPrivate: false)
    }

    private static func url(for data: Data) -> URL {
        URL(string: "https://media.example/\(key(for: data))")!
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

final class StubbedFiles: URLProtocol {
    nonisolated(unsafe) private static var files: [URL: Data] = [:]
    nonisolated(unsafe) private(set) static var requested: [URL] = []
    private static let lock = NSLock()

    static func serve(_ files: [URL: Data]) {
        lock.withLock {
            self.files = files
            requested = []
        }
    }

    override class func canInit(with request: URLRequest) -> Bool { true }

    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        guard let url = request.url else { return }
        let data = Self.lock.withLock {
            Self.requested.append(url)
            return Self.files[url]
        }
        let response = HTTPURLResponse(url: url, statusCode: data == nil ? 404 : 200, httpVersion: nil, headerFields: nil)!
        client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
        client?.urlProtocol(self, didLoad: data ?? Data())
        client?.urlProtocolDidFinishLoading(self)
    }

    override func stopLoading() {}
}
