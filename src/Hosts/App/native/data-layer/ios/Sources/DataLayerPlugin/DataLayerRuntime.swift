import DataLayerCore
import Foundation

final class DataLayerRuntime: @unchecked Sendable {
    static let shared = DataLayerRuntime()

    let transport: GrpcWebTransport?
    let store: FeedStore?
    private(set) var engine: SyncEngine?
    private(set) var outbox: Outbox?
    private(set) var state = SyncState.stopped

    var feedsChanged: ((Set<String>) -> Void)?
    var stateChanged: ((SyncState) -> Void)?
    var outboxChanged: (() -> Void)?

    private init() {
        transport = Self.configuredGateway().map { GrpcWebTransport(gateway: $0) }
        store = try? FeedStore(directory: Self.storeDirectory())
        guard let transport, let store else { return }

        outbox = Outbox(database: store.database, transport: transport, onChanged: { [weak self] in
            self?.outboxChanged?()
        })
        engine = SyncEngine(
            transport: transport, store: store,
            onFeedsChanged: { [weak self] feeds in self?.feedsChanged?(feeds) },
            onState: { [weak self] state in
                guard let self else { return }
                self.state = state
                self.stateChanged?(state)
                if state == .live { Task { await self.outbox?.drain() } }
            })
    }

    private static func configuredGateway() -> URL? {
        let info = Bundle.main.infoDictionary
        guard let scheme = info?["GsbcGatewayScheme"] as? String, !scheme.isEmpty,
              let host = info?["GsbcGateway"] as? String, !host.isEmpty else { return nil }
        return URL(string: "\(scheme)://\(host)")
    }

    private static func storeDirectory() -> URL {
        FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("DataLayer")
    }
}
