import AuthenticationServices
import DataLayerCore
import UIKit

@available(iOS 16.0, *)
final class PlatformPasskeys: NSObject, PasskeyAuthenticator, @unchecked Sendable,
    ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding {
    private let window: @MainActor () -> UIWindow?
    private var pending: CheckedContinuation<ASAuthorization, Error>?

    init(window: @escaping @MainActor () -> UIWindow?) {
        self.window = window
    }

    func createCredential(options: Data) async throws -> Data {
        let json = try Self.object(options)
        let rp = json["rp"] as? [String: Any]
        let user = json["user"] as? [String: Any]
        guard let rpId = rp?["id"] as? String, let challenge = Self.bytes(json["challenge"]),
              let userId = Self.bytes(user?["id"]), let name = user?["name"] as? String else {
            throw PasskeyFailure.unavailable("The church's server sent options this phone can't use.")
        }

        let provider = ASAuthorizationPlatformPublicKeyCredentialProvider(relyingPartyIdentifier: rpId)
        let request = provider.createCredentialRegistrationRequest(challenge: challenge, name: name, userID: userId)
        request.userVerificationPreference = .required
        guard let registration = try await perform(request).credential as? ASAuthorizationPlatformPublicKeyCredentialRegistration,
              let attestation = registration.rawAttestationObject else {
            throw PasskeyFailure.unavailable("This phone didn't create a passkey.")
        }

        return try Self.json([
            "id": Self.text(registration.credentialID),
            "rawId": Self.text(registration.credentialID),
            "type": "public-key",
            "authenticatorAttachment": "platform",
            "clientExtensionResults": [String: Any](),
            "response": [
                "clientDataJSON": Self.text(registration.rawClientDataJSON),
                "attestationObject": Self.text(attestation),
                "transports": ["internal", "hybrid"],
            ],
        ])
    }

    func assert(options: Data) async throws -> Data {
        let json = try Self.object(options)
        guard let rpId = json["rpId"] as? String, let challenge = Self.bytes(json["challenge"]) else {
            throw PasskeyFailure.unavailable("The church's server sent options this phone can't use.")
        }

        let provider = ASAuthorizationPlatformPublicKeyCredentialProvider(relyingPartyIdentifier: rpId)
        let request = provider.createCredentialAssertionRequest(challenge: challenge)
        request.userVerificationPreference = .required
        request.allowedCredentials = (json["allowCredentials"] as? [[String: Any]] ?? [])
            .compactMap { Self.bytes($0["id"]) }
            .map { ASAuthorizationPlatformPublicKeyCredentialDescriptor(credentialID: $0) }
        guard let assertion = try await perform(request).credential as? ASAuthorizationPlatformPublicKeyCredentialAssertion else {
            throw PasskeyFailure.unavailable("This phone didn't use a passkey.")
        }

        return try Self.json([
            "id": Self.text(assertion.credentialID),
            "rawId": Self.text(assertion.credentialID),
            "type": "public-key",
            "authenticatorAttachment": "platform",
            "clientExtensionResults": [String: Any](),
            "response": [
                "clientDataJSON": Self.text(assertion.rawClientDataJSON),
                "authenticatorData": Self.text(assertion.rawAuthenticatorData),
                "signature": Self.text(assertion.signature),
                "userHandle": Self.text(assertion.userID),
            ],
        ])
    }

    @MainActor
    private func perform(_ request: ASAuthorizationRequest) async throws -> ASAuthorization {
        try await withCheckedThrowingContinuation { continuation in
            pending = continuation
            let controller = ASAuthorizationController(authorizationRequests: [request])
            controller.delegate = self
            controller.presentationContextProvider = self
            controller.performRequests()
        }
    }

    func authorizationController(controller: ASAuthorizationController, didCompleteWithAuthorization authorization: ASAuthorization) {
        pending?.resume(returning: authorization)
        pending = nil
    }

    func authorizationController(controller: ASAuthorizationController, didCompleteWithError error: Error) {
        let failure: Error = switch (error as? ASAuthorizationError)?.code {
        case .canceled: PasskeyFailure.cancelled
        case .notHandled, .failed:
            PasskeyFailure.unavailable("This phone has no passkey for the church yet, or passkeys aren't set up. Check iCloud Keychain is on.")
        default: PasskeyFailure.unavailable(error.localizedDescription)
        }
        pending?.resume(throwing: failure)
        pending = nil
    }

    func presentationAnchor(for controller: ASAuthorizationController) -> ASPresentationAnchor {
        MainActor.assumeIsolated { window() } ?? ASPresentationAnchor()
    }

    private static func object(_ data: Data) throws -> [String: Any] {
        guard let json = try JSONSerialization.jsonObject(with: data) as? [String: Any] else {
            throw PasskeyFailure.unavailable("The church's server sent options this phone can't use.")
        }
        return json
    }

    private static func json(_ value: [String: Any]) throws -> Data { try JSONSerialization.data(withJSONObject: value) }

    private static func text(_ data: Data) -> String {
        data.base64EncodedString().replacingOccurrences(of: "+", with: "-").replacingOccurrences(of: "/", with: "_")
            .replacingOccurrences(of: "=", with: "")
    }

    private static func bytes(_ value: Any?) -> Data? {
        guard var text = value as? String else { return nil }
        text = text.replacingOccurrences(of: "-", with: "+").replacingOccurrences(of: "_", with: "/")
        text += String(repeating: "=", count: (4 - text.count % 4) % 4)
        return Data(base64Encoded: text)
    }
}
