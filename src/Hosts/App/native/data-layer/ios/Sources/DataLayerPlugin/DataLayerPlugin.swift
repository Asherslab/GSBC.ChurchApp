import Capacitor
import DataLayerCore
import UIKit

@objc(DataLayerPlugin)
public class DataLayerPlugin: CAPPlugin, CAPBridgedPlugin {
    public let identifier = "DataLayerPlugin"
    public let jsName = "DataLayer"
    public let pluginMethods: [CAPPluginMethod] = [
        CAPPluginMethod(name: "unary", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "startSync", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "stopSync", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "pull", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "status", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "readPage", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "storeInfo", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "enqueue", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "outbox", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "retryOutbox", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "discardOutbox", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "cachedMedia", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "fetchMedia", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "prefetchMedia", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "clearPrivateMedia", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "setSession", returnType: CAPPluginReturnPromise)
    ]

    private let runtime = DataLayerRuntime.shared
    private var transport: GrpcWebTransport? { runtime.transport }
    private var store: FeedStore? { runtime.store }
    private var engine: SyncEngine? { runtime.engine }
    private var outbox: Outbox? { runtime.outbox }

    override public func load() {
        runtime.feedsChanged = { [weak self] feeds in
            self?.notifyListeners("feedsChanged", data: ["feeds": Array(feeds).sorted()])
        }
        runtime.stateChanged = { [weak self] state in
            self?.notifyListeners("syncState", data: ["state": state.rawValue])
        }
        runtime.outboxChanged = { [weak self] in
            self?.notifyListeners("outboxChanged", data: [:])
        }

        NotificationCenter.default.addObserver(self, selector: #selector(appBecameActive),
                                               name: UIApplication.didBecomeActiveNotification, object: nil)
        NotificationCenter.default.addObserver(self, selector: #selector(appEnteredBackground),
                                               name: UIApplication.didEnterBackgroundNotification, object: nil)
    }

    @objc private func appBecameActive() {
        Task {
            await engine?.resume()
            await outbox?.sendWaitingNow()
        }
    }

    @objc private func appEnteredBackground() {
        DataLayerBackground.schedule()
        Task { await engine?.pause() }
    }

    @objc func unary(_ call: CAPPluginCall) {
        guard let method = call.getString("method"),
              let request = Data(base64Encoded: call.getString("request") ?? "") else {
            call.reject("unary needs a method and a base64 request")
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
        guard let engine else { return call.reject("The data layer isn't available in this build.") }
        let feeds = call.getArray("feeds", String.self) ?? []
        Task {
            do {
                try await engine.start(feeds: feeds)
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

    @objc func setSession(_ call: CAPPluginCall) {
        SessionKeychain.write(call.getString("secret"))
        Task {
            await engine?.pause()
            await engine?.resume()
            call.resolve(["signedIn": SessionKeychain.read() != nil])
        }
    }

    @objc func status(_ call: CAPPluginCall) {
        call.resolve(["state": runtime.state.rawValue, "developmentBuild": runtime.isDevelopmentBuild])
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
        Task {
            do {
                call.resolve(["id": try await outbox.enqueue(method: method, request: request)])
            } catch {
                call.reject(error.localizedDescription)
            }
        }
    }

    @objc func outbox(_ call: CAPPluginCall) {
        guard let outbox else { return call.reject("The data layer isn't available in this build.") }
        Task {
            do {
                let entries = try await outbox.entries().map { entry -> [String: Any] in
                    var json: [String: Any] = ["id": entry.id, "method": entry.method, "createdAt": entry.createdAt,
                                               "attempts": entry.attempts, "failed": entry.failed]
                    json["lastError"] = entry.lastError
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
