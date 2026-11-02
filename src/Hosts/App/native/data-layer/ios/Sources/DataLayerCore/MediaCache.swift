import CryptoKit
import Foundation

public enum MediaCacheError: Error {
    case notAMediaKey
    case contentDoesNotMatchKey
}

public final class MediaCache: @unchecked Sendable {
    public static let standardLimit = 200 * 1024 * 1024

    private let publicDirectory: URL
    private let privateDirectory: URL
    private let maximumBytes: Int
    private let session: URLSession
    private let lock = NSLock()

    public init(directory: URL, maximumBytes: Int = standardLimit, session: URLSession = .shared) throws {
        publicDirectory = directory.appendingPathComponent("public")
        privateDirectory = directory.appendingPathComponent("private")
        self.maximumBytes = maximumBytes
        self.session = session
        for folder in [publicDirectory, privateDirectory] {
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        }
    }

    public func cached(key: String, isPrivate: Bool) -> URL? {
        guard Self.isMediaKey(key) else { return nil }
        let file = location(key: key, isPrivate: isPrivate)
        guard FileManager.default.fileExists(atPath: file.path) else { return nil }
        touch(file)
        return file
    }

    public func fetch(key: String, from url: URL, isPrivate: Bool) async throws -> URL {
        if let file = cached(key: key, isPrivate: isPrivate) { return file }
        let (downloaded, response) = try await session.download(from: url)
        if let http = response as? HTTPURLResponse, !(200..<300).contains(http.statusCode) {
            try? FileManager.default.removeItem(at: downloaded)
            throw URLError(.badServerResponse)
        }
        return try store(key: key, isPrivate: isPrivate, downloadedFile: downloaded)
    }

    public func clearPrivate() {
        lock.withLock {
            for file in files(in: privateDirectory) { try? FileManager.default.removeItem(at: file) }
        }
    }

    public var totalBytes: Int {
        lock.withLock { entries().reduce(0) { $0 + $1.bytes } }
    }

    func store(key: String, isPrivate: Bool, downloadedFile: URL) throws -> URL {
        defer { try? FileManager.default.removeItem(at: downloadedFile) }
        guard Self.isMediaKey(key) else { throw MediaCacheError.notAMediaKey }
        let hash = SHA256.hash(data: try Data(contentsOf: downloadedFile, options: .mappedIfSafe))
        guard hash.map({ String(format: "%02x", $0) }).joined() == String(key.prefix(64)) else {
            throw MediaCacheError.contentDoesNotMatchKey
        }

        let file = location(key: key, isPrivate: isPrivate)
        try lock.withLock {
            if !FileManager.default.fileExists(atPath: file.path) {
                try FileManager.default.moveItem(at: downloadedFile, to: file)
            }
            touch(file)
            evictOverLimit()
        }
        return file
    }

    private func evictOverLimit() {
        var all = entries().sorted { $0.used < $1.used }
        var total = all.reduce(0) { $0 + $1.bytes }
        while total > maximumBytes, !all.isEmpty {
            let oldest = all.removeFirst()
            try? FileManager.default.removeItem(at: oldest.file)
            total -= oldest.bytes
        }
    }

    private func entries() -> [(file: URL, bytes: Int, used: Date)] {
        (files(in: publicDirectory) + files(in: privateDirectory)).compactMap { file in
            guard let values = try? file.resourceValues(forKeys: [.fileSizeKey, .contentModificationDateKey]) else { return nil }
            return (file, values.fileSize ?? 0, values.contentModificationDate ?? .distantPast)
        }
    }

    private func files(in folder: URL) -> [URL] {
        (try? FileManager.default.contentsOfDirectory(at: folder, includingPropertiesForKeys: [.fileSizeKey, .contentModificationDateKey])) ?? []
    }

    private func touch(_ file: URL) {
        try? FileManager.default.setAttributes([.modificationDate: Date()], ofItemAtPath: file.path)
    }

    private func location(key: String, isPrivate: Bool) -> URL {
        (isPrivate ? privateDirectory : publicDirectory).appendingPathComponent(key)
    }

    static func isMediaKey(_ key: String) -> Bool {
        let parts = key.split(separator: ".", omittingEmptySubsequences: false)
        guard parts.count == 2, parts[0].count == 64, (1...5).contains(parts[1].count) else { return false }
        return parts[0].allSatisfy { $0.isHexDigit && !$0.isUppercase } && parts[1].allSatisfy { $0.isLetter || $0.isNumber }
    }
}
