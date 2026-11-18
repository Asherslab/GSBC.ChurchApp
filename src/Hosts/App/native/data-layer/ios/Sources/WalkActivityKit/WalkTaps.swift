import Foundation

public enum WalkTapKind: String, Codable, CaseIterable, Sendable {
    case noJunkMail, spoke, undo, pause, resume, finish, door, open

    public var opensApp: Bool { self == .finish || self == .door || self == .open }
}

public struct WalkTap: Codable, Equatable, Sendable {
    public let seq: Int64
    public let kind: WalkTapKind
    public let at: Int64
}

public final class WalkTaps: @unchecked Sendable {
    public static let shared = WalkTaps(defaults: .standard)
    public static let arrived = Notification.Name("gsbc.walk.tapArrived")

    private static let key = "gsbc.walk.taps"
    private static let kept = 50
    private let defaults: UserDefaults
    private let lock = NSLock()

    public init(defaults: UserDefaults) {
        self.defaults = defaults
    }

    @discardableResult
    public func add(_ kind: WalkTapKind, at: Date = Date()) -> WalkTap {
        let tap: WalkTap = locked {
            var taps = read()
            let tap = WalkTap(seq: (taps.last?.seq ?? latestCleared) + 1, kind: kind, at: Int64((at.timeIntervalSince1970 * 1000).rounded()))
            taps.append(tap)
            write(Array(taps.suffix(Self.kept)), cleared: latestCleared)
            return tap
        }
        NotificationCenter.default.post(name: Self.arrived, object: nil)
        return tap
    }

    public func since(_ seq: Int64) -> [WalkTap] {
        locked { read().filter { $0.seq > seq } }
    }

    public func clear(upTo seq: Int64) {
        locked {
            write(read().filter { $0.seq > seq }, cleared: max(latestCleared, seq))
        }
    }

    private func locked<T>(_ work: () -> T) -> T {
        lock.lock()
        defer { lock.unlock() }
        return work()
    }

    private var latestCleared: Int64 { (defaults.object(forKey: Self.key + ".cleared") as? NSNumber)?.int64Value ?? 0 }

    private func read() -> [WalkTap] {
        guard let data = defaults.data(forKey: Self.key) else { return [] }
        return (try? JSONDecoder().decode([WalkTap].self, from: data)) ?? []
    }

    private func write(_ taps: [WalkTap], cleared: Int64) {
        defaults.set(try? JSONEncoder().encode(taps), forKey: Self.key)
        defaults.set(NSNumber(value: cleared), forKey: Self.key + ".cleared")
    }
}
