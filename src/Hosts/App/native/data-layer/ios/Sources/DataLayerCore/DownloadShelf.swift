import Foundation

public struct DownloadEntry: Codable, Equatable, Sendable {
    public enum State: String, Codable, Sendable { case downloading, done, failed }

    public var id: String
    public var url: String
    public var title: String
    public var fileName: String?
    public var bytes: Int64
    public var expected: Int64
    public var state: State
    public var addedAt: Int64
}

public final class DownloadShelf: @unchecked Sendable {
    public let directory: URL
    private let indexFile: URL
    private let lock = NSLock()
    private var entries: [String: DownloadEntry]

    public init(directory: URL, indexDirectory: URL) {
        self.directory = directory
        try? FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        try? FileManager.default.createDirectory(at: indexDirectory, withIntermediateDirectories: true)
        indexFile = indexDirectory.appendingPathComponent("downloads.json")
        entries = (try? JSONDecoder().decode([String: DownloadEntry].self, from: Data(contentsOf: indexFile))) ?? [:]
    }

    public var all: [DownloadEntry] {
        lock.withLock {
            let gone = entries.values.filter { entry in
                entry.state == .done && !FileManager.default.fileExists(atPath: directory.appendingPathComponent(entry.fileName ?? "").path)
            }
            if !gone.isEmpty {
                gone.forEach { entries[$0.id] = nil }
                save()
            }
            return entries.values.sorted { $0.addedAt > $1.addedAt }
        }
    }

    public func entry(_ id: String) -> DownloadEntry? { lock.withLock { entries[id] } }

    public func fileURL(_ id: String) -> URL? {
        guard let entry = entry(id), entry.state == .done, let name = entry.fileName else { return nil }
        let file = directory.appendingPathComponent(name)
        return FileManager.default.fileExists(atPath: file.path) ? file : nil
    }

    public func started(_ id: String, url: String, title: String) {
        change(id) { entry in
            entry = DownloadEntry(id: id, url: url, title: title, fileName: nil, bytes: 0, expected: 0, state: .downloading,
                                  addedAt: Int64(Date().timeIntervalSince1970 * 1000))
        }
    }

    public func progressed(_ id: String, written: Int64, expected: Int64) {
        change(id) { entry in
            guard entry != nil else { return }
            entry?.bytes = written
            entry?.expected = max(expected, 0)
        }
    }

    @discardableResult
    public func finished(_ id: String, movingFrom location: URL, extension fileExtension: String) throws -> URL {
        let name = freeName(for: entry(id)?.title ?? id, extension: fileExtension)
        var file = directory.appendingPathComponent(name)
        try FileManager.default.moveItem(at: location, to: file)
        var values = URLResourceValues()
        values.isExcludedFromBackup = true
        try? file.setResourceValues(values)
        let size = (try? FileManager.default.attributesOfItem(atPath: file.path)[.size] as? Int64) ?? 0
        change(id) { entry in
            entry?.fileName = name
            entry?.bytes = size
            entry?.expected = size
            entry?.state = .done
        }
        return file
    }

    public func failed(_ id: String) {
        change(id) { entry in entry?.state = .failed }
    }

    public func remove(_ id: String) {
        lock.withLock {
            if let name = entries[id]?.fileName { try? FileManager.default.removeItem(at: directory.appendingPathComponent(name)) }
            entries[id] = nil
            save()
        }
    }

    public var bytesOnDisk: Int64 { all.filter { $0.state == .done }.reduce(0) { $0 + $1.bytes } }

    static func readable(_ title: String) -> String {
        let forbidden = CharacterSet(charactersIn: "/\\:?%*|\"<>").union(.newlines).union(.controlCharacters)
        let cleaned = title.components(separatedBy: forbidden).joined(separator: " ")
            .split(separator: " ", omittingEmptySubsequences: true).joined(separator: " ")
            .trimmingCharacters(in: CharacterSet(charactersIn: ". "))
        return String((cleaned.isEmpty ? "Sermon" : cleaned).prefix(120))
    }

    private func freeName(for title: String, extension fileExtension: String) -> String {
        let base = Self.readable(title)
        var name = "\(base).\(fileExtension)"
        var number = 2
        while FileManager.default.fileExists(atPath: directory.appendingPathComponent(name).path) {
            name = "\(base) (\(number)).\(fileExtension)"
            number += 1
        }
        return name
    }

    private func change(_ id: String, _ apply: (inout DownloadEntry?) -> Void) {
        lock.withLock {
            var entry = entries[id]
            apply(&entry)
            entries[id] = entry
            save()
        }
    }

    private func save() {
        if let data = try? JSONEncoder().encode(entries) {
            try? data.write(to: indexFile, options: .atomic)
        }
    }
}
