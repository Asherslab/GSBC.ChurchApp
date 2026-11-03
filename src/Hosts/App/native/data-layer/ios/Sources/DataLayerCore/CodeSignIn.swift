import Foundation

public struct CodeSignIn: Sendable {
    public static let method = "/gsbc.accounts.Accounts/SignInWithCode"

    private let transport: GrpcWebTransport
    private let vault: SessionVault
    private let deviceName: String

    public init(transport: GrpcWebTransport, vault: SessionVault, deviceName: String) {
        self.transport = transport
        self.vault = vault
        self.deviceName = deviceName
    }

    public func signIn(email: String, code: String) async -> CeremonyOutcome {
        var request = ProtoWriter()
        request.string(1, email)
        request.string(2, code)
        request.string(3, deviceName)
        do {
            let reply = try await transport.unary(method: Self.method, message: request.data)
            guard reply.status == GrpcWebTransport.ok else { return .failed(reply.message) }
            guard let session = try Self.session(in: reply.body) else { return .failed("The church's server didn't answer properly.") }
            vault.write(session)
            return .succeeded
        } catch let error as GrpcError where error.status != GrpcWebTransport.unavailable {
            return .failed(error.message)
        } catch {
            return .failed("Couldn't reach the church's server. Check your connection and try again.")
        }
    }

    private static func session(in body: Data) throws -> String? {
        var reader = ProtoReader(body)
        while !reader.isAtEnd {
            let (field, wireType) = try reader.nextField()
            if field == 1 && wireType == 2 { return try reader.string() }
            try reader.skip(wireType: wireType)
        }
        return nil
    }
}
