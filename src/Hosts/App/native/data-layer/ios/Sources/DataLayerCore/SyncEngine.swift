import Foundation

public enum SyncState: String {
    case stopped
    case connecting
    case live
    case offline
}

public actor SyncEngine {
    private static let maximumBackoff: Double = 30

    private let transport: GrpcWebTransport
    private let store: FeedStore
    private let onFeedsChanged: @Sendable (Set<String>) -> Void
    private let onState: @Sendable (SyncState) -> Void
    private let onReset: @Sendable () -> Void

    private var feeds: [String] = []
    private var watching: Task<Void, Never>?
    private var retryWait: Task<Void, Never>?
    private var state = SyncState.stopped

    public init(transport: GrpcWebTransport, store: FeedStore,
         onFeedsChanged: @escaping @Sendable (Set<String>) -> Void,
         onState: @escaping @Sendable (SyncState) -> Void,
         onReset: @escaping @Sendable () -> Void = {}) {
        self.transport = transport
        self.store = store
        self.onFeedsChanged = onFeedsChanged
        self.onState = onState
        self.onReset = onReset
    }

    public func start(feeds: [String]) throws {
        self.feeds = feeds
        try store.register(feeds: feeds)
        resume()
    }

    public func resume() {
        guard watching == nil, !feeds.isEmpty else { return }
        watching = Task { await self.watchUntilCancelled() }
    }

    public func pause() {
        watching?.cancel()
        watching = nil
        retryWait?.cancel()
        report(.stopped)
    }

    public func pull() async throws {
        var due = feeds.isEmpty ? try store.registeredFeeds() : feeds
        while !due.isEmpty {
            let cursors = try store.cursors(for: due)
            let result = try await transport.unary(method: SyncMessages.pullMethod, message: SyncMessages.cursorsRequest(cursors))
            guard result.status == GrpcWebTransport.ok else { throw GrpcError(status: result.status, message: result.message) }
            let batches = try SyncMessages.pullReply(result.body)
            apply(batches)
            due = batches.filter(\.hasMore).map(\.feed)
        }
    }

    private func watchUntilCancelled() async {
        var failures = 0
        while !Task.isCancelled {
            report(.connecting)
            do {
                let request = SyncMessages.cursorsRequest(try store.cursors(for: feeds))
                for try await message in transport.serverStream(method: SyncMessages.watchMethod, message: request) {
                    failures = 0
                    report(.live)
                    apply(try SyncMessages.watchEvent(message).batches)
                }
            } catch {
                if Task.isCancelled { return }
            }
            report(.offline)
            failures += 1
            let backoff = min(Self.maximumBackoff, pow(2, Double(min(failures, 5)))) * Double.random(in: 0.5...1)
            let wait = Task<Void, Never> { try? await Task.sleep(nanoseconds: UInt64(backoff * 1_000_000_000)) }
            retryWait = wait
            await wait.value
            retryWait = nil
        }
    }

    public func reconnectNow() {
        retryWait?.cancel()
    }

    private func report(_ newState: SyncState) {
        guard newState != state else { return }
        state = newState
        onState(newState)
    }

    private func apply(_ batches: [FeedBatch]) {
        if batches.contains(where: \.reset) { onReset() }
        guard !batches.isEmpty, let changed = try? store.apply(batches), !changed.isEmpty else { return }
        onFeedsChanged(changed)
    }
}
