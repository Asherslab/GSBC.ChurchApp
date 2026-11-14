import Capacitor
import UIKit

extension DataLayerPlugin {
    static let keyboardMethods: [CAPPluginMethod] = [CAPPluginMethod(name: "showKeyboardAccessory", returnType: CAPPluginReturnPromise)]

    @objc func showKeyboardAccessory(_ call: CAPPluginCall) {
        let visible = call.getBool("visible") ?? true
        let refresh = call.getBool("refresh") ?? false
        DispatchQueue.main.async { [weak self] in
            guard let keyboard = self?.bridge?.plugin(withName: "Keyboard") else { return call.reject("The Keyboard plugin isn't loaded") }
            // The Keyboard plugin swaps the accessory view without asking iOS to fetch it again, so a field that is already focused keeps its old one.
            keyboard.setValue(!visible, forKey: "hideFormAccessoryBar")
            if refresh {
                self?.webView?.focusedDescendant?.reloadInputViews()
            }
            call.resolve()
        }
    }
}

private extension UIView {
    var focusedDescendant: UIView? {
        isFirstResponder ? self : subviews.lazy.compactMap(\.focusedDescendant).first
    }
}
