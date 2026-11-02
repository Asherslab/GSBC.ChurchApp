import Foundation

public struct CeremonyOutcome: Sendable, Equatable {
    public let ok: Bool
    public let error: String?

    public static let succeeded = CeremonyOutcome(ok: true, error: nil)

    public static func failed(_ error: String) -> CeremonyOutcome { CeremonyOutcome(ok: false, error: error) }
}

public enum PasskeyFailure: Error {
    case cancelled
    case unavailable(String)
}

public protocol PasskeyAuthenticator: Sendable {
    func createCredential(options: Data) async throws -> Data
    func assert(options: Data) async throws -> Data
}

public struct PasskeyCeremonies: Sendable {
    private static let prefix = "/accounts/passkeys/"

    private let gateway: URL
    private let vault: SessionVault
    private let authenticator: PasskeyAuthenticator
    private let deviceName: String
    private let configuration: URLSessionConfiguration

    public init(gateway: URL, vault: SessionVault, authenticator: PasskeyAuthenticator, deviceName: String,
                configuration: URLSessionConfiguration = .ephemeral) {
        self.gateway = gateway
        self.vault = vault
        self.authenticator = authenticator
        self.deviceName = deviceName
        self.configuration = configuration
    }

    public func signIn() async -> CeremonyOutcome {
        await run { session in
            let options = try await post("request-options", [:], session)
            let credential = try await authenticator.assert(options: options)
            let reply = try await post("sign-in", ["credential": Self.text(credential), "app": true, "device": deviceName], session)
            return try keep(reply)
        }
    }

    public func register(ticket: String?) async -> CeremonyOutcome {
        await run { session in
            let options = try await post("creation-options", ["ticket": ticket ?? NSNull()], session)
            let credential = try await authenticator.createCredential(options: options)
            let reply = try await post("register", ["credential": Self.text(credential), "ticket": ticket ?? NSNull(),
                                                    "name": deviceName, "app": true], session)
            return try keep(reply)
        }
    }

    private func run(_ ceremony: (URLSession) async throws -> CeremonyOutcome) async -> CeremonyOutcome {
        let session = URLSession(configuration: configuration)
        defer { session.invalidateAndCancel() }
        do {
            return try await ceremony(session)
        } catch PasskeyFailure.cancelled {
            return .failed("Cancelled.")
        } catch PasskeyFailure.unavailable(let reason) {
            return .failed(reason)
        } catch let refused as Refused {
            return .failed(refused.message)
        } catch {
            return .failed("Couldn't reach the church's server. Check your connection and try again.")
        }
    }

    private func keep(_ reply: Data) throws -> CeremonyOutcome {
        let json = try JSONSerialization.jsonObject(with: reply) as? [String: Any]
        if let secret = json?["session"] as? String, !secret.isEmpty {
            vault.write(secret)
        }
        return .succeeded
    }

    private func post(_ step: String, _ body: [String: Any], _ session: URLSession) async throws -> Data {
        var request = URLRequest(url: gateway.appendingPathComponent(Self.prefix + step))
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.setValue(origin, forHTTPHeaderField: "Origin")
        if let secret = vault.read() { request.setValue(secret, forHTTPHeaderField: GrpcWebTransport.sessionHeader) }
        request.httpBody = try JSONSerialization.data(withJSONObject: body)

        let (data, response) = try await session.data(for: request)
        guard let http = response as? HTTPURLResponse else { throw Refused(message: "The church's server didn't answer properly.") }
        guard (200..<300).contains(http.statusCode) else { throw Refused(message: Self.problem(data, status: http.statusCode)) }
        return data
    }

    private var origin: String {
        var components = URLComponents()
        components.scheme = gateway.scheme
        components.host = gateway.host
        components.port = gateway.port
        return components.string ?? gateway.absoluteString
    }

    private static func text(_ data: Data) -> String { String(decoding: data, as: UTF8.self) }

    private static func problem(_ data: Data, status: Int) -> String {
        let json = (try? JSONSerialization.jsonObject(with: data)) as? [String: Any]
        return json?["detail"] as? String ?? json?["title"] as? String ?? "Something went wrong (\(status))."
    }

    private struct Refused: Error {
        let message: String
    }
}
