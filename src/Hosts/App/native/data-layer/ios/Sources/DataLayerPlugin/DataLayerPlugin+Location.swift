import Capacitor
import DataLayerCore
import Foundation

extension DataLayerPlugin {
    static let locationMethods: [CAPPluginMethod] = [
        "locationStart", "locationStop", "locationState", "locationFixes", "locationClear",
    ].map { CAPPluginMethod(name: $0, returnType: CAPPluginReturnPromise) }

    func listenToLocation() {
        DispatchQueue.main.async { [weak self] in
            LocationSession.shared.onFix = { seq in
                self?.notifyListeners("locationFix", data: ["seq": seq])
            }
            LocationSession.shared.resumeIfRunning()
        }
    }

    @objc func locationStart(_ call: CAPPluginCall) {
        let purpose = call.getString("purpose") ?? ""
        DispatchQueue.main.async {
            LocationSession.shared.start(purpose: purpose) { call.resolve(["state": $0]) }
        }
    }

    @objc func locationStop(_ call: CAPPluginCall) {
        DispatchQueue.main.async {
            LocationSession.shared.stop()
            call.resolve()
        }
    }

    @objc func locationState(_ call: CAPPluginCall) {
        DispatchQueue.main.async {
            LocationSession.shared.state { call.resolve($0) }
        }
    }

    @objc func locationFixes(_ call: CAPPluginCall) {
        let since = (call.getValue("since") as? NSNumber)?.int64Value ?? 0
        DispatchQueue.main.async {
            let session = LocationSession.shared
            let found = session.journal.since(since)
            let fixes = found.fixes.map { fix -> [String: Any] in
                ["seq": fix.seq, "at": fix.at, "lat": fix.lat, "lon": fix.lon, "accuracy": fix.accuracy, "heading": fix.heading, "speed": fix.speed]
            }
            let json = (try? JSONSerialization.data(withJSONObject: ["fixes": fixes, "latest": found.latest, "running": session.running]))
                .map { String(decoding: $0, as: UTF8.self) } ?? "{\"fixes\":[],\"latest\":\(since),\"running\":false}"
            call.resolve(["json": json])
        }
    }

    @objc func locationClear(_ call: CAPPluginCall) {
        guard let upTo = (call.getValue("upTo") as? NSNumber)?.int64Value else { return call.reject("locationClear needs upTo") }
        DispatchQueue.main.async {
            LocationSession.shared.journal.clear(upTo: upTo)
            call.resolve()
        }
    }
}
