import Foundation

actor InstallProofs {
    enum Methods: Sendable {
        case only(Set<String>)
        case all
    }

    let signer: RequestSigner
    private var methods: Methods?
    private var listing: Task<Methods?, Never>?
    private var lastInTurn: Task<Void, Never>?

    init(signer: RequestSigner) {
        self.signer = signer
    }

    func require(_ method: String, refreshing: Bool, list: @escaping @Sendable () async -> Methods?) async -> Bool {
        if refreshing || methods == nil { await load(list) }
        switch methods {
        case .all: return true
        case .only(let paths): return paths.contains(method)
        case nil: return false
        }
    }

    func inTurn<T: Sendable>(_ call: @escaping @Sendable () async throws -> T) async throws -> T {
        let previous = lastInTurn
        let turn = Task {
            await previous?.value
            return try await call()
        }
        lastInTurn = Task { _ = try? await turn.value }
        return try await turn.value
    }

    private func load(_ list: @escaping @Sendable () async -> Methods?) async {
        let task = listing ?? Task { await list() }
        listing = task
        let loaded = await task.value
        if listing == task { listing = nil }
        if let loaded { methods = loaded }
    }
}
