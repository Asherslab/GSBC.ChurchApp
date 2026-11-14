import Foundation

public struct LocationFix: Codable, Equatable, Sendable {
    public var seq: Int64
    public var at: Int64
    public var lat: Double
    public var lon: Double
    public var accuracy: Double
    public var heading: Double
    public var speed: Double

    public init(seq: Int64, at: Int64, lat: Double, lon: Double, accuracy: Double, heading: Double, speed: Double) {
        self.seq = seq
        self.at = at
        self.lat = lat
        self.lon = lon
        self.accuracy = accuracy
        self.heading = heading
        self.speed = speed
    }
}

public final class LocationJournal: @unchecked Sendable {
    private struct Header: Codable { var latest: Int64 }

    public static let defaultCap = 20_000

    private let file: URL
    private let cap: Int
    private let lock = NSLock()
    private var fixes: [LocationFix] = []
    private var latestSeq: Int64 = 0
    private var handle: FileHandle?

    public init(directory: URL, cap: Int = LocationJournal.defaultCap) {
        try? FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        file = directory.appendingPathComponent("fixes.jsonl")
        self.cap = max(cap, 1)
        load()
    }

    deinit { try? handle?.close() }

    public var latest: Int64 { lock.withLock { latestSeq } }

    public var count: Int { lock.withLock { fixes.count } }

    @discardableResult
    public func append(at: Int64, lat: Double, lon: Double, accuracy: Double, heading: Double, speed: Double) -> LocationFix {
        lock.withLock {
            latestSeq += 1
            let fix = LocationFix(seq: latestSeq, at: at, lat: lat, lon: lon, accuracy: accuracy, heading: heading, speed: speed)
            fixes.append(fix)
            if fixes.count > cap + cap / 10 {
                fixes.removeFirst(fixes.count - cap)
                rewrite()
            } else {
                appendLine(fix)
            }
            return fix
        }
    }

    public func since(_ seq: Int64) -> (fixes: [LocationFix], latest: Int64) {
        lock.withLock {
            let start = fixes.firstIndex { $0.seq > seq } ?? fixes.endIndex
            return (Array(fixes[start...]), latestSeq)
        }
    }

    public func clear(upTo seq: Int64) {
        lock.withLock {
            let before = fixes.count
            fixes.removeAll { $0.seq <= seq }
            if fixes.count != before { rewrite() }
        }
    }

    private func load() {
        guard let data = try? Data(contentsOf: file) else { return rewrite() }
        let complete = data.lastIndex(of: UInt8(ascii: "\n")).map { data[...$0] } ?? Data()
        let decoder = JSONDecoder()
        for line in complete.split(separator: UInt8(ascii: "\n")) {
            if let fix = try? decoder.decode(LocationFix.self, from: line) {
                fixes.append(fix)
                latestSeq = max(latestSeq, fix.seq)
            } else if let header = try? decoder.decode(Header.self, from: line) {
                latestSeq = max(latestSeq, header.latest)
            }
        }
        fixes.sort { $0.seq < $1.seq }
        if fixes.count > cap { fixes.removeFirst(fixes.count - cap) }
        if complete.count != data.count || fixes.count > cap { rewrite() }
    }

    private func appendLine(_ fix: LocationFix) {
        guard let line = try? JSONEncoder().encode(fix) else { return }
        if handle == nil {
            handle = try? FileHandle(forWritingTo: file)
            _ = try? handle?.seekToEnd()
        }
        guard let handle else { return rewrite() }
        do {
            try handle.write(contentsOf: line + Data([UInt8(ascii: "\n")]))
        } catch {
            rewrite()
        }
    }

    private func rewrite() {
        try? handle?.close()
        handle = nil
        let encoder = JSONEncoder()
        var data = (try? encoder.encode(Header(latest: latestSeq))) ?? Data()
        data.append(UInt8(ascii: "\n"))
        for fix in fixes {
            guard let line = try? encoder.encode(fix) else { continue }
            data.append(line)
            data.append(UInt8(ascii: "\n"))
        }
        try? data.write(to: file, options: [.atomic, .completeFileProtectionUntilFirstUserAuthentication])
    }
}
