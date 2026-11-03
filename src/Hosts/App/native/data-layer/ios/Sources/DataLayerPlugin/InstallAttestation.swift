import CryptoKit
import DataLayerCore
import DeviceCheck
import Foundation

actor InstallAttestation: RequestSigner {
    private static let keyIdDefault = "gsbc.install.keyId"
    private static let attestedDefault = "gsbc.install.attested"
    static let keyHeader = "gsbc-install"
    static let assertionHeader = "gsbc-assertion"

    private let defaults = UserDefaults.standard
    private var attesting: Task<String?, Never>?

    nonisolated var isSupported: Bool { DCAppAttestService.shared.isSupported }

    func headers(method: String, body: Data) async -> [String: String] {
        guard let keyId = attestedKeyId() else { return [:] }
        let clientDataHash = Data(SHA256.hash(data: InstallMessages.clientData(method: method, body: body)))
        guard let assertion = try? await DCAppAttestService.shared.generateAssertion(keyId, clientDataHash: clientDataHash) else {
            return [:]
        }
        return [Self.keyHeader: keyId, Self.assertionHeader: assertion.base64EncodedString()]
    }

    func ensureAttested(using transport: GrpcWebTransport) async -> Bool {
        guard isSupported else { return false }
        if attestedKeyId() != nil { return true }
        if attesting == nil { attesting = Task { await attest(using: transport) } }
        let keyId = await attesting?.value
        attesting = nil
        return keyId != nil
    }

    private func attestedKeyId() -> String? {
        defaults.bool(forKey: Self.attestedDefault) ? defaults.string(forKey: Self.keyIdDefault) : nil
    }

    private func attest(using transport: GrpcWebTransport) async -> String? {
        do {
            let keyId: String
            if let existing = defaults.string(forKey: Self.keyIdDefault) {
                keyId = existing
            } else {
                keyId = try await DCAppAttestService.shared.generateKey()
                defaults.set(keyId, forKey: Self.keyIdDefault)
            }

            let begun = try await transport.unary(method: InstallMessages.beginMethod, message: Data())
            guard begun.status == GrpcWebTransport.ok else { return nil }
            let challenge = try InstallMessages.challenge(from: begun.body)
            let attestation = try await DCAppAttestService.shared.attestKey(keyId, clientDataHash: Data(SHA256.hash(data: challenge)))
            let attested = try await transport.unary(
                method: InstallMessages.attestMethod,
                message: InstallMessages.attestRequest(keyId: keyId, challenge: challenge, attestation: attestation))
            guard attested.status == GrpcWebTransport.ok else {
                if attested.status == Self.permissionDenied { defaults.removeObject(forKey: Self.keyIdDefault) }
                return nil
            }
            defaults.set(true, forKey: Self.attestedDefault)
            return keyId
        } catch let error as DCError where error.code == .invalidKey {
            defaults.removeObject(forKey: Self.keyIdDefault)
            return nil
        } catch {
            return nil
        }
    }

    private static let permissionDenied = 7
}
