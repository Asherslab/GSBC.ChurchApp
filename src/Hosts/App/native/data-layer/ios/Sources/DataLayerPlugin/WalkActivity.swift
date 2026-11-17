import ActivityKit
import Foundation
import WalkActivityKit

enum WalkActivity {
    private static let staleAfter: TimeInterval = 180

    static var available: Bool {
        guard #available(iOS 17.0, *) else { return false }
        return ActivityAuthorizationInfo().areActivitiesEnabled
    }

    static func show(name: String, state: WalkActivityState, alert: String?) async {
        guard #available(iOS 17.0, *), available else { return }
        let content = ActivityContent(state: state, staleDate: Date().addingTimeInterval(staleAfter))
        let current = Activity<WalkActivityAttributes>.activities
        if let activity = current.first {
            for extra in current.dropFirst() { await extra.end(nil, dismissalPolicy: .immediate) }
            await activity.update(content, alertConfiguration: alert.map {
                AlertConfiguration(title: LocalizedStringResource(stringLiteral: state.street), body: LocalizedStringResource(stringLiteral: $0), sound: .default)
            })
        } else {
            _ = try? Activity.request(attributes: WalkActivityAttributes(name: name), content: content)
        }
    }

    static func end() async {
        guard #available(iOS 17.0, *) else { return }
        for activity in Activity<WalkActivityAttributes>.activities {
            await activity.end(nil, dismissalPolicy: .immediate)
        }
    }

    static func decode(_ json: String) -> WalkActivityState? {
        try? JSONDecoder().decode(WalkActivityState.self, from: Data(json.utf8))
    }
}
