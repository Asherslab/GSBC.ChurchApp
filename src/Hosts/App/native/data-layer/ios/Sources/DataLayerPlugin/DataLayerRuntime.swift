import DataLayerCore
import Foundation
import Network

final class DataLayerRuntime: @unchecked Sendable {
    static let shared = DataLayerRuntime()

    static let vault = SessionKeychain()
    static let installs = InstallAttestation()

    let transport: GrpcWebTransport?
    let store: FeedStore?
    let media: MediaCache?
    let shelf: UploadShelf?
    private(set) var engine: SyncEngine?
    private(set) var outbox: Outbox?
    private(set) var state = SyncState.stopped
    let isDevelopmentBuild = (Bundle.main.infoDictionary?["GsbcDevelopmentBuild"] as? String) == "YES"

    var feedsChanged: ((Set<String>) -> Void)?
    var stateChanged: ((SyncState) -> Void)?
    var outboxChanged: (() -> Void)?
    var sessionChanged: (() -> Void)?
    private(set) var privateFeeds: [String] = []

    var signedIn: Bool { Self.vault.read() != nil }

    private let network = NWPathMonitor()
    private var wasOnline = true

    private init() {
        transport = Self.configuredGateway().map {
            GrpcWebTransport(gateway: $0, sessionSecret: { DataLayerRuntime.vault.read() },
                             onSessionEnded: { DataLayerRuntime.shared.sessionEndedElsewhere() },
                             signer: DataLayerRuntime.installs)
        }
        store = try? FeedStore(directory: Self.storeDirectory())
        let media = try? MediaCache(directory: Self.mediaDirectory())
        self.media = media
        let shelf = try? UploadShelf(directory: Self.uploadsDirectory())
        self.shelf = shelf
        guard let transport, let store else { return }

        if let kept = try? Outbox.keptFiles(in: store.database) {
            shelf?.sweep(keeping: kept)
        }
        outbox = Outbox(database: store.database, transport: transport, shelf: shelf, onChanged: { [weak self] in
            self?.outboxChanged?()
        })
        engine = SyncEngine(
            transport: transport, store: store,
            onFeedsChanged: { [weak self] feeds in self?.feedsChanged?(feeds) },
            onState: { [weak self] state in
                guard let self else { return }
                self.state = state
                self.stateChanged?(state)
                if state == .live { Task { await self.outbox?.sendWaitingNow() } }
            },
            onReset: { media?.clearPrivate() })

        network.pathUpdateHandler = { [weak self] path in
            guard let self else { return }
            let online = path.status == .satisfied
            defer { self.wasOnline = online }
            guard online, !self.wasOnline else { return }
            Task { await self.engine?.reconnectNow() }
        }
        network.start(queue: DispatchQueue(label: "au.com.baptist.app.datalayer.network"))
    }

    func startSync(feeds: [String], privateFeeds: [String]) async throws {
        self.privateFeeds = privateFeeds
        try await engine?.start(feeds: feeds)
    }

    func sessionStarted() async {
        await clearPrivateData()
    }

    func forgetSession() async {
        Self.vault.write(nil)
        await clearPrivateData()
    }

    private func sessionEndedElsewhere() {
        guard signedIn else { return }
        Task { await forgetSession() }
    }

    private func clearPrivateData() async {
        await engine?.pause()
        if let cleared = try? store?.clear(feeds: privateFeeds), !cleared.isEmpty { feedsChanged?(cleared) }
        media?.clearPrivate()
        await engine?.resume()
        sessionChanged?()
    }

    private static func configuredGateway() -> URL? {
        let info = Bundle.main.infoDictionary
        guard let scheme = info?["GsbcGatewayScheme"] as? String, !scheme.isEmpty,
              let host = info?["GsbcGateway"] as? String, !host.isEmpty else { return nil }
        return URL(string: "\(scheme)://\(host)")
    }

    private static func mediaDirectory() -> URL {
        FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask)[0].appendingPathComponent("Media")
    }

    private static func uploadsDirectory() -> URL {
        storeDirectory().appendingPathComponent("Uploads")
    }

    private static func storeDirectory() -> URL {
        FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("DataLayer")
    }
}
