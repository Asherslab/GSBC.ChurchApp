import Capacitor
import Foundation

extension DataLayerPlugin {
    static let compassMethods: [CAPPluginMethod] = [
        "compassStart", "compassStop",
    ].map { CAPPluginMethod(name: $0, returnType: CAPPluginReturnPromise) }

    @objc func compassStart(_ call: CAPPluginCall) {
        Compass.shared.onHeading = { [weak self] heading, accuracy in
            self?.notifyListeners("compass", data: ["heading": heading, "accuracy": accuracy])
        }
        Compass.shared.start()
        call.resolve()
    }

    @objc func compassStop(_ call: CAPPluginCall) {
        Compass.shared.stop()
        call.resolve()
    }
}
