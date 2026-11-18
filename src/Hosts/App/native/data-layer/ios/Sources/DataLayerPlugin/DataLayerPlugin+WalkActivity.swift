import Capacitor
import Foundation
import WalkActivityKit

extension DataLayerPlugin {
    static let walkActivityMethods: [CAPPluginMethod] = [
        "walkActivityShow", "walkActivityEnd", "walkTaps", "walkTapsClear",
    ].map { CAPPluginMethod(name: $0, returnType: CAPPluginReturnPromise) }

    func listenToWalkTaps() {
        NotificationCenter.default.addObserver(forName: WalkTaps.arrived, object: nil, queue: .main) { [weak self] _ in
            self?.notifyListeners("walkTap", data: [:], retainUntilConsumed: true)
        }
    }

    @objc func walkActivityShow(_ call: CAPPluginCall) {
        guard let json = call.getString("state"), let state = WalkActivity.decode(json) else { return call.reject("walkActivityShow needs a state") }
        let name = call.getString("name") ?? ""
        let alert = call.getString("alert")
        Task {
            await WalkActivity.show(name: name, state: state, alert: alert)
            call.resolve(["shown": WalkActivity.available])
        }
    }

    @objc func walkActivityEnd(_ call: CAPPluginCall) {
        Task {
            await WalkActivity.end()
            call.resolve()
        }
    }

    @objc func walkTaps(_ call: CAPPluginCall) {
        let since = (call.getValue("since") as? NSNumber)?.int64Value ?? 0
        let taps = WalkTaps.shared.since(since).map { tap -> [String: Any] in ["seq": tap.seq, "kind": tap.kind.rawValue, "at": tap.at] }
        let json = (try? JSONSerialization.data(withJSONObject: taps)).map { String(decoding: $0, as: UTF8.self) } ?? "[]"
        call.resolve(["json": json])
    }

    @objc func walkTapsClear(_ call: CAPPluginCall) {
        guard let upTo = (call.getValue("upTo") as? NSNumber)?.int64Value else { return call.reject("walkTapsClear needs upTo") }
        WalkTaps.shared.clear(upTo: upTo)
        call.resolve()
    }
}
