#if canImport(ActivityKit)
import ActivityKit
#endif
import Foundation

public enum WalkPhase: String, Codable, Hashable, Sendable {
    case walking, paused, finding, done
}

public enum WalkTurn: String, Codable, Hashable, Sendable {
    case left, right, straight, back, none
}

public struct WalkActivityState: Codable, Hashable, Sendable {
    public var phase: WalkPhase
    public var status: String
    public var totals: String
    public var street: String
    public var side: String
    public var toGo: Int
    public var turn: WalkTurn
    public var next: String
    public var said: String
    public var canUndo: Bool
    public var doorknock: Bool

    public init(phase: WalkPhase, status: String, totals: String, street: String, side: String, toGo: Int, turn: WalkTurn,
                next: String, said: String, canUndo: Bool, doorknock: Bool) {
        self.phase = phase
        self.status = status
        self.totals = totals
        self.street = street
        self.side = side
        self.toGo = toGo
        self.turn = turn
        self.next = next
        self.said = said
        self.canUndo = canUndo
        self.doorknock = doorknock
    }
}

#if canImport(ActivityKit)
@available(iOS 16.2, *)
public struct WalkActivityAttributes: ActivityAttributes {
    public typealias ContentState = WalkActivityState

    public var name: String

    public init(name: String) {
        self.name = name
    }
}
#endif
