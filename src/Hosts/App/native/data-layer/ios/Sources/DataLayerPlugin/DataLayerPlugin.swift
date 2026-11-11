import Capacitor
import DataLayerCore
import UIKit

@objc(DataLayerPlugin)
public class DataLayerPlugin: CAPPlugin, CAPBridgedPlugin {
    public let identifier = "DataLayerPlugin"
    public let jsName = "DataLayer"
    public let pluginMethods: [CAPPluginMethod] = DataLayerPlugin.dataMethods + DataLayerPlugin.audioMethods

    private static let dataMethods: [CAPPluginMethod] = [
        "unary",
        "startSync",
        "stopSync",
        "pull",
        "status",
        "readPage",
        "storeInfo",
        "enqueue",
        "outbox",
        "retryOutbox",
        "discardOutbox",
        "discardQuietOutbox",
        "stageUpload",
        "appendUpload",
        "finishUpload",
        "forgetUpload",
        "takeCrashes",
        "cachedMedia",
        "fetchMedia",
        "prefetchMedia",
        "clearPrivateMedia",
        "signIn",
        "registerPasskey",
        "signInWithCode",
        "signOut",
        "preference",
        "setPreference",
        "proveInstall",
        "pushPermission",
        "registerPush",
        "setBadge",
    ].map { CAPPluginMethod(name: $0, returnType: CAPPluginReturnPromise) }

    private static let preferences = UserDefaults.standard
    private static let preferencePrefix = "gsbc.preference."
    private static let connectToolMethod = "/gsbc.accounts.Accounts/ConnectTool"

    private let runtime = DataLayerRuntime.shared
    private var transport: GrpcWebTransport? { runtime.transport }
    private var store: FeedStore? { runtime.store }
    private var engine: SyncEngine? { runtime.engine }
    private var outbox: Outbox? { runtime.outbox }

    override public func load() {
        listenToAudio()
        runtime.feedsChanged = { [weak self] feeds in
            self?.notifyListeners("feedsChanged", data: ["feeds": Array(feeds).sorted()])
        }
        runtime.stateChanged = { [weak self] state in
            self?.notifyListeners("syncState", data: ["state": state.rawValue])
        }
        runtime.outboxChanged = { [weak self] in
            self?.notifyListeners("outboxChanged", data: [:])
        }
        runtime.sessionChanged = { [weak self] in
            self?.notifyListeners("sessionChanged", data: ["signedIn": self?.runtime.signedIn ?? false])
        }
        DataLayerPush.shared.onLinkOpened = { [weak self] path in
            self?.notifyListeners("linkOpened", data: ["path": path], retainUntilConsumed: true)
        }
        CrashCollector.shared.crashesArrived = { [weak self] in
            self?.notifyListeners("crashesArrived", data: [:], retainUntilConsumed: true)
        }
        CrashCollector.shared.start()
        NotificationCenter.default.addObserver(self, selector: #selector(linkOpened(_:)),
                                               name: .capacitorSceneOpenUniversalLink, object: nil)

        NotificationCenter.default.addObserver(self, selector: #selector(appBecameActive),
                                               name: UIApplication.didBecomeActiveNotification, object: nil)
        NotificationCenter.default.addObserver(self, selector: #selector(appEnteredBackground),
                                               name: UIApplication.didEnterBackgroundNotification, object: nil)
    }

    @objc private func appBecameActive() {
        notifyListeners("appStateChanged", data: ["active": true])
        Task {
            await engine?.resume()
            await outbox?.sendWaitingNow()
        }
    }

    @objc private func linkOpened(_ notification: Notification) {
        guard let url = notification.userInfo?["url"] as? URL, url.host == transport?.gateway.host,
              let components = URLComponents(url: url, resolvingAgainstBaseURL: false) else { return }
        let path = components.path + (components.percentEncodedQuery.map { "?" + $0 } ?? "")
        notifyListeners("linkOpened", data: ["path": path], retainUntilConsumed: true)
    }

    @objc private func appEnteredBackground() {
        notifyListeners("appStateChanged", data: ["active": false])
        DataLayerBackground.schedule()
        Task { await engine?.pause() }
    }

    @objc func unary(_ call: CAPPluginCall) {
        guard let method = call.getString("method"),
              let request = Data(base64Encoded: call.getString("request") ?? "") else {
            call.reject("unary needs a method and a base64 request")
            return
        }
        guard method != CodeSignIn.method else {
            call.reject("Sign in with a code through signInWithCode, so the session stays on this phone.")
            return
        }
        guard method != Self.connectToolMethod else {
            call.reject("Connect a tool in the admin panel on a computer.")
            return
        }
        guard let transport else {
            resolve(call, GrpcResult(status: GrpcWebTransport.unavailable, message: "This build has no gateway configured.", body: Data()))
            return
        }

        Task {
            do {
                resolve(call, try await transport.unary(method: method, message: request))
            } catch let error as GrpcError {
                resolve(call, GrpcResult(status: error.status, message: error.message, body: Data()))
            } catch {
                resolve(call, GrpcResult(status: GrpcWebTransport.unavailable, message: error.localizedDescription, body: Data()))
            }
        }
    }

    @objc func startSync(_ call: CAPPluginCall) {
        guard engine != nil else { return call.reject("The data layer isn't available in this build.") }
        let feeds = call.getArray("feeds", String.self) ?? []
        let privateFeeds = call.getArray("privateFeeds", String.self) ?? []
        Task {
            do {
                try await runtime.startSync(feeds: feeds, privateFeeds: privateFeeds)
                call.resolve()
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    @objc func stopSync(_ call: CAPPluginCall) {
        Task {
            await engine?.pause()
            call.resolve()
        }
    }

    @objc func pull(_ call: CAPPluginCall) {
        guard let engine else { return call.reject("The data layer isn't available in this build.") }
        Task {
            do {
                try await engine.pull()
                call.resolve()
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    @objc func signIn(_ call: CAPPluginCall) {
        ceremony(call) { await $0.signIn() }
    }

    @objc func registerPasskey(_ call: CAPPluginCall) {
        let ticket = call.getString("ticket")
        ceremony(call) { await $0.register(ticket: ticket) }
    }

    @objc func signInWithCode(_ call: CAPPluginCall) {
        guard let transport else { return resolve(call, .failed("This build has no gateway configured.")) }
        let email = call.getString("email") ?? ""
        let code = call.getString("code") ?? ""
        let wasSignedIn = runtime.signedIn
        Task {
            let outcome = await CodeSignIn(transport: transport, vault: DataLayerRuntime.vault, deviceName: UIDevice.current.model)
                .signIn(email: email, code: code)
            if outcome.ok && !wasSignedIn { await runtime.sessionStarted() }
            resolve(call, outcome)
        }
    }

    @objc func signOut(_ call: CAPPluginCall) {
        Task {
            await runtime.forgetSession()
            call.resolve()
        }
    }

    @objc func proveInstall(_ call: CAPPluginCall) {
        guard let transport else { return call.resolve(["proven": false, "supported": false]) }
        Task {
            let proven = await DataLayerRuntime.installs.ensureAttested(using: transport)
            call.resolve(["proven": proven, "supported": DataLayerRuntime.installs.isSupported])
        }
    }

    @objc func pushPermission(_ call: CAPPluginCall) {
        Task { call.resolve(["permission": await DataLayerPush.shared.permission()]) }
    }

    @objc func registerPush(_ call: CAPPluginCall) {
        Task {
            var result: [String: Any] = ["permission": await DataLayerPush.shared.permission()]
            if let token = await DataLayerPush.shared.requestToken() {
                result["token"] = token
                result["environment"] = DataLayerPush.shared.environment
                result["appId"] = Bundle.main.bundleIdentifier ?? ""
            }
            result["permission"] = await DataLayerPush.shared.permission()
            call.resolve(result)
        }
    }

    @objc func setBadge(_ call: CAPPluginCall) {
        DataLayerPush.shared.setBadge(call.getInt("count") ?? 0)
        call.resolve()
    }

    @objc func status(_ call: CAPPluginCall) {
        let info = Bundle.main.infoDictionary
        call.resolve(["state": runtime.state.rawValue, "developmentBuild": runtime.isDevelopmentBuild, "signedIn": runtime.signedIn,
                      "version": info?["CFBundleShortVersionString"] as? String ?? "", "build": info?["CFBundleVersion"] as? String ?? "",
                      "platform": "ios", "osMajor": ProcessInfo.processInfo.operatingSystemVersion.majorVersion,
                      "gateway": transport?.gateway.absoluteString ?? ""])
    }

    @objc func preference(_ call: CAPPluginCall) {
        guard let key = call.getString("key") else { return call.reject("preference needs a key") }
        var result: [String: Any] = [:]
        result["value"] = Self.preferences.string(forKey: Self.preferencePrefix + key)
        call.resolve(result)
    }

    @objc func setPreference(_ call: CAPPluginCall) {
        guard let key = call.getString("key") else { return call.reject("setPreference needs a key") }
        if let value = call.getString("value") {
            Self.preferences.set(value, forKey: Self.preferencePrefix + key)
        } else {
            Self.preferences.removeObject(forKey: Self.preferencePrefix + key)
        }
        call.resolve()
    }

    private func ceremony(_ call: CAPPluginCall, _ run: @escaping (PasskeyCeremonies) async -> CeremonyOutcome) {
        guard let gateway = transport?.gateway else { return resolve(call, .failed("This build has no gateway configured.")) }
        guard #available(iOS 16.0, *) else { return resolve(call, .failed("Passkeys need iOS 16 or later.")) }
        let passkeys = PlatformPasskeys(window: { [weak self] in self?.bridge?.viewController?.view.window })
        let ceremonies = PasskeyCeremonies(gateway: gateway, vault: DataLayerRuntime.vault, authenticator: passkeys,
                                           deviceName: UIDevice.current.model)
        let wasSignedIn = runtime.signedIn
        Task {
            let outcome = await run(ceremonies)
            if outcome.ok && !wasSignedIn { await runtime.sessionStarted() }
            resolve(call, outcome)
        }
    }

    private func resolve(_ call: CAPPluginCall, _ outcome: CeremonyOutcome) {
        var result: [String: Any] = ["ok": outcome.ok]
        result["error"] = outcome.error
        call.resolve(result)
    }

    @objc func readPage(_ call: CAPPluginCall) {
        guard let store else { return call.reject("The data layer isn't available in this build.") }
        guard let feed = call.getString("feed") else { return call.reject("readPage needs a feed") }
        do {
            let records = try store.page(
                feed: feed,
                order: FeedOrder(rawValue: call.getString("order") ?? "") ?? .sortKey,
                descending: call.getBool("descending") ?? false,
                limit: call.getInt("limit") ?? 50,
                offset: call.getInt("offset") ?? 0,
                groupKey: call.getString("groupKey"))
            call.resolve(["json": Self.json(records.map(Self.record))])
        } catch {
            call.reject(error.localizedDescription)
        }
    }

    @objc func storeInfo(_ call: CAPPluginCall) {
        guard let store else { return call.reject("The data layer isn't available in this build.") }
        do {
            let feeds = try store.info().map { ["feed": $0.feed, "rows": $0.rows, "bytes": $0.bytes, "cursor": $0.cursor] as [String: Any] }
            call.resolve(["json": Self.json(["fileBytes": store.fileBytes, "feeds": feeds])])
        } catch {
            call.reject(error.localizedDescription)
        }
    }

    @objc func enqueue(_ call: CAPPluginCall) {
        guard let outbox else { return call.reject("The data layer isn't available in this build.") }
        guard let method = call.getString("method"), let request = Data(base64Encoded: call.getString("request") ?? "") else {
            return call.reject("enqueue needs a method and a base64 request")
        }
        guard let uploads = Self.uploads(call.getArray("uploads") as? [[String: Any]] ?? []) else {
            return call.reject("enqueue's uploads need a file, its name, type and size, three calls and where the file goes")
        }
        Task {
            do {
                call.resolve(["id": try await outbox.enqueue(method: method, request: request, quiet: call.getBool("quiet") ?? false, uploads: uploads)])
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    @objc func stageUpload(_ call: CAPPluginCall) {
        guard let shelf = runtime.shelf else { return call.reject("This build can't keep files for sending.") }
        do {
            call.resolve(["token": try shelf.begin(extension: call.getString("extension") ?? "")])
        } catch {
            call.reject(error.localizedDescription)
        }
    }

    @objc func appendUpload(_ call: CAPPluginCall) {
        guard let shelf = runtime.shelf, let token = call.getString("token"),
              let data = Data(base64Encoded: call.getString("data") ?? "") else { return call.reject("appendUpload needs a token and base64 data") }
        do {
            try shelf.append(token, data)
            call.resolve()
        } catch {
            call.reject(error.localizedDescription)
        }
    }

    @objc func finishUpload(_ call: CAPPluginCall) {
        guard let shelf = runtime.shelf, let token = call.getString("token") else { return call.reject("finishUpload needs a token") }
        do {
            let staged = try shelf.finish(token)
            call.resolve(["token": staged.token, "bytes": staged.bytes, "sha256": staged.sha256, "path": staged.url.path])
        } catch {
            call.reject(error.localizedDescription)
        }
    }

    @objc func forgetUpload(_ call: CAPPluginCall) {
        guard let shelf = runtime.shelf, let token = call.getString("token") else { return call.resolve() }
        Task {
            if await (try? outbox?.isKept(token)) != true { shelf.forget(token) }
            call.resolve()
        }
    }

    @objc func outbox(_ call: CAPPluginCall) {
        guard let outbox else { return call.reject("The data layer isn't available in this build.") }
        Task {
            do {
                let entries = try await outbox.entries().map { entry -> [String: Any] in
                    var json: [String: Any] = ["id": entry.id, "method": entry.method, "createdAt": entry.createdAt,
                                               "attempts": entry.attempts, "failed": entry.failed, "waitsForUpload": entry.waitsForUpload]
                    json["lastError"] = entry.lastError
                    if let upload = entry.upload {
                        json["upload"] = ["file": upload.file, "fileName": upload.fileName, "bytes": upload.bytes] as [String: Any]
                    }
                    return json
                }
                call.resolve(["json": Self.json(entries)])
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    @objc func retryOutbox(_ call: CAPPluginCall) {
        changeOutbox(call) { outbox, id in try await outbox.retry(id: id) }
    }

    @objc func discardOutbox(_ call: CAPPluginCall) {
        changeOutbox(call) { outbox, id in try await outbox.discard(id: id) }
    }

    @objc func discardQuietOutbox(_ call: CAPPluginCall) {
        guard let outbox else { return call.resolve() }
        Task {
            do {
                try await outbox.discardQuiet()
                call.resolve()
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    @objc func takeCrashes(_ call: CAPPluginCall) {
        let crashes = CrashCollector.shared.inbox.take().map { crash -> [String: Any] in
            ["type": crash.type, "frames": crash.frames, "crashedAt": crash.crashedAt,
             "appVersion": crash.appVersion, "appBuild": crash.appBuild]
        }
        call.resolve(["json": Self.json(crashes)])
    }

    @objc func cachedMedia(_ call: CAPPluginCall) {
        guard let media = runtime.media, let key = call.getString("key") else { return call.reject("cachedMedia needs a key") }
        var result: [String: Any] = [:]
        result["path"] = media.cached(key: key, isPrivate: call.getBool("isPrivate") ?? false)?.path
        call.resolve(result)
    }

    @objc func fetchMedia(_ call: CAPPluginCall) {
        guard let media = runtime.media, let key = call.getString("key"),
              let url = URL(string: call.getString("url") ?? "") else { return call.reject("fetchMedia needs a key and a url") }
        let isPrivate = call.getBool("isPrivate") ?? false
        Task {
            do {
                call.resolve(["path": try await media.fetch(key: key, from: url, isPrivate: isPrivate).path])
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    @objc func prefetchMedia(_ call: CAPPluginCall) {
        guard let media = runtime.media else { return call.reject("The media cache isn't available") }
        let items = (call.getArray("items") as? [[String: Any]] ?? []).compactMap { item -> PrefetchItem? in
            guard let key = item["key"] as? String, let url = URL(string: item["url"] as? String ?? ""),
                  let bytes = (item["bytes"] as? NSNumber)?.intValue else { return nil }
            return PrefetchItem(key: key, url: url, bytes: bytes, isPrivate: item["isPrivate"] as? Bool ?? false)
        }
        Task { call.resolve(["fetched": await media.prefetch(items)]) }
    }

    @objc func clearPrivateMedia(_ call: CAPPluginCall) {
        runtime.media?.clearPrivate()
        call.resolve()
    }

    private func changeOutbox(_ call: CAPPluginCall, _ change: @escaping (Outbox, String) async throws -> Void) {
        guard let outbox, let id = call.getString("id") else { return call.reject("Needs an outbox entry id.") }
        Task {
            do {
                try await change(outbox, id)
                call.resolve()
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    private static func uploads(_ items: [[String: Any]]) -> [QueuedUpload]? {
        var uploads: [QueuedUpload] = []
        for item in items {
            guard let file = item["file"] as? String, let fileName = item["fileName"] as? String,
                  let contentType = item["contentType"] as? String, let bytes = (item["bytes"] as? NSNumber)?.int64Value,
                  let find = queuedCall(item["find"]), let begin = queuedCall(item["begin"]), let complete = queuedCall(item["complete"]),
                  let paths = (item["paths"] as? [String])?.compactMap(UploadMessages.path), !paths.isEmpty else { return nil }
            uploads.append(QueuedUpload(file: file, fileName: fileName, contentType: contentType, bytes: bytes,
                                        find: find, begin: begin, complete: complete, paths: paths))
        }
        return uploads
    }

    private static func queuedCall(_ value: Any?) -> QueuedCall? {
        guard let call = value as? [String: Any], let method = call["method"] as? String,
              let request = Data(base64Encoded: call["request"] as? String ?? "") else { return nil }
        return QueuedCall(method: method, request: request)
    }

    private func resolve(_ call: CAPPluginCall, _ result: GrpcResult) {
        call.resolve(["status": result.status, "message": result.message, "response": result.body.base64EncodedString()])
    }

    private static func record(_ record: StoredRecord) -> [String: Any] {
        var json: [String: Any] = ["id": record.id, "version": record.version, "payload": record.payload.base64EncodedString()]
        json["sortKey"] = record.sortKey
        json["startsAt"] = record.startsAt
        json["endsAt"] = record.endsAt
        json["groupKey"] = record.groupKey
        return json
    }

    private static func json(_ value: Any) -> String {
        guard let data = try? JSONSerialization.data(withJSONObject: value) else { return "null" }
        return String(decoding: data, as: UTF8.self)
    }
}
