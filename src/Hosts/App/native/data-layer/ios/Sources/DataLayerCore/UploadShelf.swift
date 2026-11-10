import CryptoKit
import Foundation

public struct StagedFile: Equatable {
    public let token: String
    public let bytes: Int64
    public let sha256: String
    public let url: URL
}

public final class UploadShelf: @unchecked Sendable {
    public let directory: URL
    private let lock = NSLock()
    private static let readChunk = 1 << 20

    public init(directory: URL) throws {
        self.directory = directory
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
    }

    public func begin(extension wanted: String) throws -> String {
        let suffix = wanted.lowercased().filter { $0.isLetter || $0.isNumber }.prefix(5)
        let token = UUID().uuidString.lowercased() + (suffix.isEmpty ? "" : ".\(suffix)")
        guard FileManager.default.createFile(atPath: file(token).path, contents: nil) else {
            throw ProtoError(reason: "couldn't keep the file on this phone")
        }
        return token
    }

    public func append(_ token: String, _ data: Data) throws {
        try lock.withLock {
            let handle = try FileHandle(forWritingTo: try existing(token))
            defer { try? handle.close() }
            try handle.seekToEnd()
            try handle.write(contentsOf: data)
        }
    }

    public func finish(_ token: String) throws -> StagedFile {
        let url = try existing(token)
        let handle = try FileHandle(forReadingFrom: url)
        defer { try? handle.close() }
        var hash = SHA256()
        var bytes: Int64 = 0
        while let chunk = try handle.read(upToCount: Self.readChunk), !chunk.isEmpty {
            hash.update(data: chunk)
            bytes += Int64(chunk.count)
        }
        let sha256 = hash.finalize().map { String(format: "%02x", $0) }.joined()
        return StagedFile(token: token, bytes: bytes, sha256: sha256, url: url)
    }

    public func file(_ token: String) -> URL {
        directory.appendingPathComponent((token as NSString).lastPathComponent)
    }

    public func has(_ token: String) -> Bool {
        FileManager.default.fileExists(atPath: file(token).path)
    }

    public func forget(_ token: String) {
        try? FileManager.default.removeItem(at: file(token))
    }

    public func sweep(keeping kept: Set<String>) {
        let files = (try? FileManager.default.contentsOfDirectory(atPath: directory.path)) ?? []
        for name in files where !kept.contains(name) {
            forget(name)
        }
    }

    private func existing(_ token: String) throws -> URL {
        let url = file(token)
        guard FileManager.default.fileExists(atPath: url.path) else { throw ProtoError(reason: "that file is no longer on this phone") }
        return url
    }
}
