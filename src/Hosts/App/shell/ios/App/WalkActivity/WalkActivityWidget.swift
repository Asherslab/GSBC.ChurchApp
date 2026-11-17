import ActivityKit
import SwiftUI
import WalkActivityKit
import WidgetKit

@main
struct WalkActivityBundle: WidgetBundle {
    var body: some Widget {
        WalkActivityWidget()
    }
}

struct WalkActivityWidget: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: WalkActivityAttributes.self) { context in
            LockScreenWalk(state: context.state, stale: context.isStale)
                .activityBackgroundTint(Palette.navy)
                .activitySystemActionForegroundColor(.white)
                .widgetURL(WalkLink.url)
        } dynamicIsland: { context in
            let state = context.state
            return DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    TurnSign(state: state, size: 48, tone: .island)
                        .padding(.leading, 6)
                        .padding(.top, 4)
                }
                DynamicIslandExpandedRegion(.trailing) {
                    if state.phase == .walking || state.phase == .paused {
                        VStack(alignment: .trailing, spacing: 2) {
                            Text("\(state.toGo)").font(.montserrat(.bold, 19)).monospacedDigit().contentTransition(.numericText())
                            Text("to go").font(.montserrat(.semibold, 13)).foregroundStyle(Palette.onMastheadQuiet)
                        }
                        .foregroundStyle(.white)
                        .padding(.trailing, 6)
                        .padding(.top, 18)
                    }
                }
                DynamicIslandExpandedRegion(.center) {
                    VStack(alignment: .leading, spacing: 2) {
                        Text(state.street).font(.montserrat(.bold, 19)).lineLimit(1).minimumScaleFactor(0.7)
                        Text(state.phase == .paused ? "Paused · \(state.side)" : state.side).font(.montserrat(.semibold, 13)).lineLimit(1).minimumScaleFactor(0.85)
                    }
                    .foregroundStyle(.white)
                    .frame(maxWidth: .infinity, alignment: .leading)
                }
                DynamicIslandExpandedRegion(.bottom) {
                    Group {
                        if state.said.isEmpty {
                            WalkButtons(state: state, tone: .island)
                        } else {
                            SaidLine(state: state, tone: .island)
                        }
                    }
                    .dynamicTypeSize(...DynamicTypeSize.xLarge)
                }
            } compactLeading: {
                HStack(spacing: 4) {
                    TurnGlyph(state: state).frame(width: 16, height: 16)
                    if state.phase == .walking || state.phase == .paused {
                        Text("\(state.toGo)").font(.montserrat(.bold, 14)).monospacedDigit()
                    }
                }
                .foregroundStyle(Palette.brightAqua)
            } compactTrailing: {
                Text(state.phase == .paused ? "Paused" : state.street)
                    .font(.montserrat(.semibold, 13))
                    .lineLimit(1)
                    .minimumScaleFactor(0.75)
                    .frame(maxWidth: 96)
                    .foregroundStyle(.white)
            } minimal: {
                TurnGlyph(state: state)
                    .frame(width: 16, height: 16)
                    .foregroundStyle(Palette.brightAqua)
            }
            .widgetURL(WalkLink.url)
            .keylineTint(Palette.brightAqua)
        }
    }
}

struct LockScreenWalk: View {
    let state: WalkActivityState
    let stale: Bool

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            if state.said.isEmpty {
                HStack {
                    Text(stale ? "Open the app to keep marking streets" : state.status)
                    Spacer(minLength: 8)
                    Text(state.totals).monospacedDigit().contentTransition(.numericText())
                }
                .font(.montserrat(.semibold, 11.5))
                .foregroundStyle(Palette.onMastheadQuiet)
                .lineLimit(1)
            } else {
                SaidLine(state: state, tone: .lockScreen)
            }
            HStack(spacing: 12) {
                TurnSign(state: state, size: 58, tone: .lockScreen)
                VStack(alignment: .leading, spacing: 1) {
                    Text(state.street).font(.montserrat(.bold, 22)).lineLimit(1).minimumScaleFactor(0.7)
                    Text(sideLine).font(.montserrat(.semibold, 14)).lineLimit(2).minimumScaleFactor(0.85)
                    if !state.next.isEmpty {
                        Text(state.next).font(.montserrat(.regular, 12.5)).foregroundStyle(Palette.beige).lineLimit(1).minimumScaleFactor(0.8)
                    }
                }
                .foregroundStyle(.white)
            }
            WalkButtons(state: state, tone: .lockScreen)
        }
        .padding(.horizontal, 14)
        .padding(.vertical, 10)
        .dynamicTypeSize(...DynamicTypeSize.xLarge)
    }

    private var sideLine: String {
        switch state.phase {
        case .walking: return "\(state.side) · \(state.toGo) to go"
        case .paused: return "Paused · \(state.side) · \(state.toGo) to go"
        case .finding, .done: return state.side
        }
    }
}

enum Tone {
    case lockScreen, island
}

struct TurnSign: View {
    let state: WalkActivityState
    let size: CGFloat
    let tone: Tone
    @Environment(\.isLuminanceReduced) private var dimmed

    var body: some View {
        VStack(spacing: 1) {
            TurnGlyph(state: state).frame(width: size * 0.42, height: size * 0.42)
            if tone == .lockScreen && (state.phase == .walking || state.phase == .paused) {
                Text("\(state.toGo)").font(.montserrat(.bold, 11)).monospacedDigit().contentTransition(.numericText())
            }
        }
        .foregroundStyle(ink)
        .frame(width: size, height: size)
        .background(ground, in: shape)
        .overlay { if dimmed { shape.stroke(.white, lineWidth: 1.5) } }
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(state.next.isEmpty ? state.street : "\(state.toGo) to go. \(state.next)")
    }

    private var shape: RoundedRectangle { RoundedRectangle(cornerRadius: size * 0.24, style: .continuous) }

    private var paused: Bool { state.phase == .paused }

    private var ink: Color {
        if dimmed || (paused && tone == .island) { return .white }
        return tone == .lockScreen ? Palette.navy : Palette.onBrightAqua
    }

    private var ground: Color {
        if dimmed { return .clear }
        switch tone {
        case .lockScreen: return paused ? Palette.beige : .white
        case .island: return paused ? Color.white.opacity(0.16) : Palette.brightAqua
        }
    }
}

struct TurnGlyph: View {
    let state: WalkActivityState

    var body: some View {
        Image(name).resizable().scaledToFit()
    }

    private var name: String {
        switch state.phase {
        case .finding: return "footprints"
        case .done: return "check"
        case .walking, .paused:
            switch state.turn {
            case .left: return "arrow-left"
            case .right: return "arrow-right"
            case .back: return "rotate-ccw"
            case .straight: return "arrow-up"
            case .none: return "flag"
            }
        }
    }
}

struct SaidLine: View {
    let state: WalkActivityState
    let tone: Tone

    var body: some View {
        if state.canUndo {
            Button(intent: WalkTapIntent(.undo)) { line }
                .buttonStyle(.plain)
                .accessibilityLabel("\(state.said) Undo")
        } else {
            line
        }
    }

    private var line: some View {
        HStack(spacing: 10) {
            Text(state.said).font(.montserrat(.semibold, 12.5)).foregroundStyle(.white).lineLimit(1).minimumScaleFactor(0.8)
            Spacer(minLength: 0)
            if state.canUndo {
                Text("Undo")
                    .font(.montserrat(.bold, 12.5))
                    .foregroundStyle(tone == .island ? Palette.brightAqua : .white)
                    .underline(tone == .lockScreen)
            }
        }
        .padding(.horizontal, 12)
        .frame(height: tone == .island ? 44 : 26)
        .background(tone == .island ? Palette.brightAqua.opacity(0.16) : Color.white.opacity(0.14), in: RoundedRectangle(cornerRadius: tone == .island ? 22 : 11, style: .continuous))
        .contentShape(Rectangle())
    }
}

struct WalkButtons: View {
    let state: WalkActivityState
    let tone: Tone

    var body: some View {
        HStack(spacing: 6) {
            if state.doorknock {
                Button(intent: WalkOpenIntent(.door)) { WideLabel(icon: "door-open", text: "At a door", tone: tone) }.buttonStyle(.plain)
            } else {
                Button(intent: WalkTapIntent(.noJunkMail)) { WideLabel(icon: "circle-off", text: "No junk mail", tone: tone) }.buttonStyle(.plain)
            }
            Button(intent: WalkTapIntent(.spoke)) { WideLabel(icon: "message-square", text: "Spoke", tone: tone) }
                .buttonStyle(.plain)
                .accessibilityLabel("Spoke to someone")
            if state.phase == .paused {
                Button(intent: WalkTapIntent(.resume)) { RoundLabel(icon: "play", finish: false, tone: tone) }
                    .buttonStyle(.plain)
                    .accessibilityLabel("Carry on")
            } else {
                Button(intent: WalkTapIntent(.pause)) { RoundLabel(icon: "pause", finish: false, tone: tone) }
                    .buttonStyle(.plain)
                    .accessibilityLabel("Pause")
            }
            Button(intent: WalkOpenIntent(.finish)) { RoundLabel(icon: "check", finish: true, tone: tone) }
                .buttonStyle(.plain)
                .accessibilityLabel("Finish")
        }
    }
}

struct WideLabel: View {
    let icon: String
    let text: String
    let tone: Tone

    var body: some View {
        HStack(spacing: 5) {
            Image(icon).resizable().scaledToFit().frame(width: 15, height: 15)
            Text(text).font(.montserrat(.semibold, 12.5)).lineLimit(1).minimumScaleFactor(0.8)
        }
        .foregroundStyle(.white)
        .padding(.horizontal, 8)
        .frame(maxWidth: .infinity, minHeight: 44)
        .background(Color.white.opacity(tone == .island ? 0.16 : 0.14), in: Capsule())
    }
}

struct RoundLabel: View {
    let icon: String
    let finish: Bool
    let tone: Tone
    @Environment(\.isLuminanceReduced) private var dimmed

    var body: some View {
        Image(icon).resizable().scaledToFit().frame(width: 16, height: 16)
            .foregroundStyle(finish && !dimmed ? (tone == .island ? Palette.onBrightAqua : Palette.navy) : .white)
            .frame(width: 44, height: 44)
            .background(ground, in: Circle())
            .overlay { if finish && dimmed { Circle().stroke(.white, lineWidth: 1.5) } }
    }

    private var ground: Color {
        if finish { return dimmed ? .clear : tone == .island ? Palette.brightAqua : .white }
        return Color.white.opacity(tone == .island ? 0.16 : 0.14)
    }
}

enum Palette {
    static let navy = Color(red: 0x2a / 255, green: 0x3d / 255, blue: 0x5e / 255)
    static let beige = Color(red: 0xd5 / 255, green: 0xc9 / 255, blue: 0xb7 / 255)
    static let brightAqua = Color(red: 0x2e / 255, green: 0xc4 / 255, blue: 0xb6 / 255)
    static let onBrightAqua = Color(red: 0x06 / 255, green: 0x25 / 255, blue: 0x22 / 255)
    static let onMastheadQuiet = Color.white.opacity(0.76)
}

enum WalkLink {
    static let url = URL(string: "gsbc-walk://outreach/walking")
}

extension Font {
    enum Montserrat: String {
        case regular = "Montserrat-Regular"
        case semibold = "Montserrat-SemiBold"
        case bold = "Montserrat-Bold"
    }

    static func montserrat(_ weight: Montserrat, _ size: CGFloat) -> Font {
        .custom(weight.rawValue, size: size, relativeTo: .body)
    }
}
