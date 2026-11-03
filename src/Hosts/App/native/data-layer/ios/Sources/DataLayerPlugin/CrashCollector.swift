import DataLayerCore
import Foundation
import MetricKit

final class CrashCollector: NSObject, MXMetricManagerSubscriber {
    static let shared = CrashCollector()

    let inbox = CrashInbox()
    var crashesArrived: (() -> Void)?

    func start() {
        MXMetricManager.shared.add(self)
    }

    func didReceive(_ payloads: [MXDiagnosticPayload]) {
        let version = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? ""
        let crashes = payloads.flatMap { payload in
            (payload.crashDiagnostics ?? []).map { crash in
                CrashSummary.from(callStackTree: crash.callStackTree.jsonRepresentation(),
                                  exceptionType: crash.exceptionType?.intValue, signal: crash.signal?.intValue,
                                  exceptionName: Self.exceptionName(of: crash), crashedAt: payload.timeStampEnd,
                                  appVersion: version, appBuild: crash.metaData.applicationBuildVersion)
            }
        }
        inbox.add(crashes)
        if !crashes.isEmpty {
            crashesArrived?()
        }
    }

    private static func exceptionName(of crash: MXCrashDiagnostic) -> String? {
        if #available(iOS 17.0, *) {
            return crash.exceptionReason?.exceptionName
        }
        return nil
    }
}
