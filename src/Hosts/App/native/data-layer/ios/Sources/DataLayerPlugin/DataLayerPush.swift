import Foundation
import UIKit
import UserNotifications

public final class DataLayerPush: NSObject, UNUserNotificationCenterDelegate, @unchecked Sendable {
    public static let shared = DataLayerPush()
    private static let backgroundBudget: TimeInterval = 25

    private let lock = NSLock()
    private var pendingLink: String?
    private var token: String?
    private var tokenWaiters: [CheckedContinuation<String?, Never>] = []

    var onLinkOpened: ((String) -> Void)? {
        didSet { if let link = takePendingLink() { onLinkOpened?(link) } }
    }

    var environment: String {
        (Bundle.main.infoDictionary?["GsbcApsEnvironment"] as? String) == "development" ? "sandbox" : "production"
    }

    public static func start() {
        UNUserNotificationCenter.current().delegate = shared
    }

    public func registered(deviceToken: Data) {
        let hex = deviceToken.map { String(format: "%02x", $0) }.joined()
        finishWaiting(with: hex)
    }

    public func failedToRegister() {
        finishWaiting(with: nil)
    }

    public func receivedInBackground(completion: @escaping (UIBackgroundFetchResult) -> Void) {
        let runtime = DataLayerRuntime.shared
        let work = Task {
            do {
                try await runtime.engine?.pull()
                await runtime.outbox?.drain()
                completion(.newData)
            } catch {
                completion(.failed)
            }
        }
        DispatchQueue.main.asyncAfter(deadline: .now() + Self.backgroundBudget) { work.cancel() }
    }

    func permission() async -> String {
        switch await UNUserNotificationCenter.current().notificationSettings().authorizationStatus {
        case .authorized, .provisional, .ephemeral: return "granted"
        case .denied: return "denied"
        default: return "ask"
        }
    }

    func requestToken() async -> String? {
        let granted = (try? await UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .badge, .sound])) ?? false
        guard granted else { return nil }
        return await withCheckedContinuation { continuation in
            lock.lock()
            tokenWaiters.append(continuation)
            lock.unlock()
            DispatchQueue.main.async { UIApplication.shared.registerForRemoteNotifications() }
        }
    }

    func setBadge(_ count: Int) {
        if #available(iOS 16.0, *) {
            UNUserNotificationCenter.current().setBadgeCount(count)
        } else {
            DispatchQueue.main.async { UIApplication.shared.applicationIconBadgeNumber = count }
        }
    }

    public func userNotificationCenter(_ center: UNUserNotificationCenter, willPresent notification: UNNotification,
                                       withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void) {
        completionHandler([.banner, .list, .sound])
    }

    public func userNotificationCenter(_ center: UNUserNotificationCenter, didReceive response: UNNotificationResponse,
                                       withCompletionHandler completionHandler: @escaping () -> Void) {
        if let link = response.notification.request.content.userInfo["link"] as? String, link.hasPrefix("/") {
            if let open = onLinkOpened {
                open(link)
            } else {
                lock.lock()
                pendingLink = link
                lock.unlock()
            }
        }
        completionHandler()
    }

    private func takePendingLink() -> String? {
        lock.lock()
        defer { lock.unlock() }
        let link = pendingLink
        pendingLink = nil
        return link
    }

    private func finishWaiting(with token: String?) {
        lock.lock()
        if let token { self.token = token }
        let waiters = tokenWaiters
        tokenWaiters = []
        lock.unlock()
        for waiter in waiters { waiter.resume(returning: token) }
    }
}
