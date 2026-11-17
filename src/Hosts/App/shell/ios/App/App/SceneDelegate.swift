import UIKit
import Capacitor
import WalkActivityKit

class SceneDelegate: UIResponder, UIWindowSceneDelegate {
    var window: UIWindow?

    func scene(_ scene: UIScene, willConnectTo session: UISceneSession, options connectionOptions: UIScene.ConnectionOptions) {
        guard let windowScene = scene as? UIWindowScene else { return }

        window = UIWindow(windowScene: windowScene)
        window?.rootViewController = CAPBridgeViewController()
        window?.makeKeyAndVisible()

        SceneDelegateProxy.shared.scene(scene, willConnectTo: session, options: connectionOptions)
        openWalk(connectionOptions.urlContexts)
    }

    func scene(_ scene: UIScene, openURLContexts URLContexts: Set<UIOpenURLContext>) {
        guard !openWalk(URLContexts) else { return }
        SceneDelegateProxy.shared.scene(scene, openURLContexts: URLContexts)
    }

    @discardableResult
    private func openWalk(_ contexts: Set<UIOpenURLContext>) -> Bool {
        guard contexts.contains(where: { $0.url.scheme == "gsbc-walk" }) else { return false }
        WalkTaps.shared.add(.open)
        return true
    }

    func scene(_ scene: UIScene, continue userActivity: NSUserActivity) {
        SceneDelegateProxy.shared.scene(scene, continue: userActivity)
    }
}
