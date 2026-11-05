import Foundation

public struct PlaybackRecord: Codable, Equatable, Sendable {
    public var id: String
    public var position: Double
    public var duration: Double
    public var heardAt: Int64
    public var marks: [String: Int64]
    public var seq: Int64
}

public final class PlaybackJournal: @unchecked Sendable {
    private struct Stored: Codable {
        var seq: Int64 = 0
        var records: [String: PlaybackRecord] = [:]
    }

    private let file: URL
    private let lock = NSLock()
    private var stored: Stored

    public init(directory: URL) {
        try? FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        file = directory.appendingPathComponent("playback-journal.json")
        stored = (try? JSONDecoder().decode(Stored.self, from: Data(contentsOf: file))) ?? Stored()
    }

    public func heard(_ id: String, position: Double, duration: Double, at: Date = Date()) {
        change(id) { record in
            record.position = max(0, position)
            if duration > 0 { record.duration = duration }
            record.heardAt = Self.milliseconds(at)
        }
    }

    public func mark(_ id: String, _ mark: String, at: Int64) {
        change(id) { record in record.marks[mark] = max(record.marks[mark] ?? 0, at) }
    }

    public func record(_ id: String) -> PlaybackRecord? {
        lock.withLock { stored.records[id] }
    }

    public func since(_ seq: Int64) -> (records: [PlaybackRecord], latest: Int64) {
        lock.withLock {
            (stored.records.values.filter { $0.seq > seq }.sorted { $0.seq < $1.seq }, stored.seq)
        }
    }

    private func change(_ id: String, _ apply: (inout PlaybackRecord) -> Void) {
        lock.withLock {
            stored.seq += 1
            var record = stored.records[id] ?? PlaybackRecord(id: id, position: 0, duration: 0, heardAt: 0, marks: [:], seq: 0)
            apply(&record)
            record.seq = stored.seq
            stored.records[id] = record
            if let data = try? JSONEncoder().encode(stored) {
                try? data.write(to: file, options: [.atomic, .completeFileProtectionUntilFirstUserAuthentication])
            }
        }
    }

    static func milliseconds(_ date: Date) -> Int64 { Int64((date.timeIntervalSince1970 * 1000).rounded()) }
}
