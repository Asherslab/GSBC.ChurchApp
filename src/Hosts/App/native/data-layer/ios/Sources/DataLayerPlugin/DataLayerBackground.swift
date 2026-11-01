import BackgroundTasks
import Foundation

public enum DataLayerBackground {
    private static let interval: TimeInterval = 15 * 60

    private static var refreshIdentifier: String {
        "\(Bundle.main.bundleIdentifier ?? "au.com.baptist.churchapp").refresh"
    }

    public static func register() {
        BGTaskScheduler.shared.register(forTaskWithIdentifier: refreshIdentifier, using: nil) { task in
            schedule()
            let runtime = DataLayerRuntime.shared
            let work = Task {
                do {
                    try await runtime.engine?.pull()
                    await runtime.outbox?.drain()
                    task.setTaskCompleted(success: true)
                } catch {
                    task.setTaskCompleted(success: false)
                }
            }
            task.expirationHandler = { work.cancel() }
        }
    }

    public static func schedule() {
        let request = BGAppRefreshTaskRequest(identifier: refreshIdentifier)
        request.earliestBeginDate = Date(timeIntervalSinceNow: interval)
        try? BGTaskScheduler.shared.submit(request)
    }
}
