import Foundation

public struct CrashSummary: Codable, Equatable, Sendable {
    public let type: String
    public let frames: [String]
    public let crashedAt: Int64
    public let appVersion: String
    public let appBuild: String

    public static let maximumFrames = 12

    public static func from(callStackTree: Data, exceptionType: Int?, signal: Int?, exceptionName: String?,
                            crashedAt: Date, appVersion: String, appBuild: String) -> CrashSummary {
        CrashSummary(type: describe(exceptionType: exceptionType, signal: signal, exceptionName: exceptionName),
                     frames: frames(of: callStackTree),
                     crashedAt: Int64(crashedAt.timeIntervalSince1970 * 1000),
                     appVersion: appVersion, appBuild: appBuild)
    }

    static func describe(exceptionType: Int?, signal: Int?, exceptionName: String?) -> String {
        let mach = exceptionType.map { machNames[$0] ?? "EXC_\($0)" }
        let posix = signal.map { signalNames[$0] ?? "signal \($0)" }
        let kind = [mach, posix.map { mach == nil ? $0 : "(\($0))" }].compactMap { $0 }.joined(separator: " ")
        return [exceptionName, kind.isEmpty ? nil : kind].compactMap { $0 }.joined(separator: ": ").ifEmpty("Native crash")
    }

    static func frames(of callStackTree: Data) -> [String] {
        guard let tree = try? JSONSerialization.jsonObject(with: callStackTree) as? [String: Any],
              let stacks = tree["callStacks"] as? [[String: Any]],
              let crashed = stacks.first(where: { $0["threadAttributed"] as? Bool == true }) ?? stacks.first,
              var frame = (crashed["callStackRootFrames"] as? [[String: Any]])?.first else { return [] }

        var frames: [String] = []
        while frames.count < maximumFrames {
            let binary = frame["binaryName"] as? String ?? "?"
            let offset = (frame["offsetIntoBinaryTextSegment"] as? NSNumber)?.int64Value ?? 0
            frames.append("\(binary) +\(offset)")
            guard let caller = (frame["subFrames"] as? [[String: Any]])?.first else { break }
            frame = caller
        }
        return frames
    }

    private static let machNames: [Int: String] = [
        1: "EXC_BAD_ACCESS", 2: "EXC_BAD_INSTRUCTION", 3: "EXC_ARITHMETIC", 4: "EXC_EMULATION", 5: "EXC_SOFTWARE",
        6: "EXC_BREAKPOINT", 10: "EXC_CRASH", 11: "EXC_RESOURCE", 12: "EXC_GUARD", 13: "EXC_CORPSE_NOTIFY",
    ]

    private static let signalNames: [Int: String] = [
        4: "SIGILL", 5: "SIGTRAP", 6: "SIGABRT", 8: "SIGFPE", 9: "SIGKILL", 10: "SIGBUS", 11: "SIGSEGV", 13: "SIGPIPE",
    ]
}

public final class CrashInbox: @unchecked Sendable {
    private static let key = "gsbc.crashes.pending"
    private static let maximumPending = 20

    private let defaults: UserDefaults
    private let lock = NSLock()

    public init(defaults: UserDefaults = .standard) {
        self.defaults = defaults
    }

    public func add(_ crashes: [CrashSummary]) {
        guard !crashes.isEmpty else { return }
        lock.withLock {
            save(Array((load() + crashes).suffix(Self.maximumPending)))
        }
    }

    public func take() -> [CrashSummary] {
        lock.withLock {
            let pending = load()
            defaults.removeObject(forKey: Self.key)
            return pending
        }
    }

    private func load() -> [CrashSummary] {
        defaults.data(forKey: Self.key).flatMap { try? JSONDecoder().decode([CrashSummary].self, from: $0) } ?? []
    }

    private func save(_ crashes: [CrashSummary]) {
        defaults.set(try? JSONEncoder().encode(crashes), forKey: Self.key)
    }
}

private extension String {
    func ifEmpty(_ fallback: String) -> String { isEmpty ? fallback : self }
}
