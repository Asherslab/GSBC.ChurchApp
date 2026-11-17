import AppIntents
import WalkActivityKit

@available(iOS 17.0, *)
struct WalkTapIntent: LiveActivityIntent {
    static let title: LocalizedStringResource = "Mark it on your walk"
    static let isDiscoverable = false

    @Parameter(title: "Mark")
    var kind: String

    init() {}

    init(_ kind: WalkTapKind) {
        self.kind = kind.rawValue
    }

    func perform() async throws -> some IntentResult {
        WalkTaps.shared.add(WalkTapKind(rawValue: kind) ?? .open)
        return .result()
    }
}

@available(iOS 17.0, *)
struct WalkOpenIntent: LiveActivityIntent {
    static let title: LocalizedStringResource = "Open your walk"
    static let isDiscoverable = false
    static let openAppWhenRun = true

    @Parameter(title: "Mark")
    var kind: String

    init() {}

    init(_ kind: WalkTapKind) {
        self.kind = kind.rawValue
    }

    func perform() async throws -> some IntentResult {
        WalkTaps.shared.add(WalkTapKind(rawValue: kind) ?? .open)
        return .result()
    }
}
