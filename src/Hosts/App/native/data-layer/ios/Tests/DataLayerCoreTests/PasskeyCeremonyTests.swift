import DataLayerCore
import Foundation
import XCTest

final class PasskeyCeremonyTests: XCTestCase {
    private static let secret = "app-session-secret"

    override func tearDown() {
        StubGateway.replies = [:]
        StubGateway.requests = []
    }

    func testSigningInKeepsTheSessionInTheVaultAndHandsBackOnlyTheOutcome() async throws {
        StubGateway.replies = [
            "/accounts/passkeys/request-options": (200, #"{"challenge":"AAAA","rpId":"dev.baptist.com.au"}"#),
            "/accounts/passkeys/sign-in": (200, #"{"signedIn":true,"session":"\#(Self.secret)"}"#),
        ]
        let vault = MemoryVault()

        let outcome = await ceremonies(vault).signIn()

        XCTAssertEqual(outcome, .succeeded)
        XCTAssertEqual(vault.read(), Self.secret)
        XCTAssertFalse(String(describing: outcome).contains(Self.secret))
        let signIn = try XCTUnwrap(StubGateway.requests.last)
        XCTAssertEqual(signIn.value(forHTTPHeaderField: "Origin"), "https://dev.baptist.com.au")
        let body = try XCTUnwrap(JSONSerialization.jsonObject(with: signIn.body) as? [String: Any])
        XCTAssertEqual(body["app"] as? Bool, true)
    }

    func testAddingAPasskeyWhileSignedInSendsTheSessionAndKeepsIt() async {
        StubGateway.replies = [
            "/accounts/passkeys/creation-options": (200, #"{"challenge":"AAAA"}"#),
            "/accounts/passkeys/register": (200, #"{"signedIn":true,"session":null}"#),
        ]
        let vault = MemoryVault()
        vault.write(Self.secret)

        let outcome = await ceremonies(vault).register(ticket: nil)

        XCTAssertEqual(outcome, .succeeded)
        XCTAssertEqual(vault.read(), Self.secret)
        XCTAssertEqual(StubGateway.requests.map { $0.value(forHTTPHeaderField: GrpcWebTransport.sessionHeader) }, [Self.secret, Self.secret])
    }

    func testARefusalIsShownInTheServersWordsAndLeavesThePhoneSignedOut() async {
        StubGateway.replies = [
            "/accounts/passkeys/request-options": (200, #"{"challenge":"AAAA"}"#),
            "/accounts/passkeys/sign-in": (403, #"{"detail":"This account is disabled."}"#),
        ]
        let vault = MemoryVault()

        let outcome = await ceremonies(vault).signIn()

        XCTAssertEqual(outcome, .failed("This account is disabled."))
        XCTAssertNil(vault.read())
    }

    private func ceremonies(_ vault: MemoryVault) -> PasskeyCeremonies {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [StubGateway.self]
        return PasskeyCeremonies(gateway: URL(string: "https://dev.baptist.com.au")!, vault: vault,
                                 authenticator: AlwaysApproves(), deviceName: "iPhone", configuration: configuration)
    }
}

private struct AlwaysApproves: PasskeyAuthenticator {
    func createCredential(options: Data) async throws -> Data { Data(#"{"id":"credential"}"#.utf8) }
    func assert(options: Data) async throws -> Data { Data(#"{"id":"credential"}"#.utf8) }
}

private final class MemoryVault: SessionVault, @unchecked Sendable {
    private var secret: String?
    func read() -> String? { secret }
    func write(_ secret: String?) { self.secret = secret }
}

private struct RecordedRequest {
    let headers: [String: String]
    let body: Data

    func value(forHTTPHeaderField name: String) -> String? { headers.first { $0.key.caseInsensitiveCompare(name) == .orderedSame }?.value }
}

private final class StubGateway: URLProtocol {
    nonisolated(unsafe) static var replies: [String: (Int, String)] = [:]
    nonisolated(unsafe) static var requests: [RecordedRequest] = []

    override class func canInit(with request: URLRequest) -> Bool { true }
    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        Self.requests.append(RecordedRequest(headers: request.allHTTPHeaderFields ?? [:], body: Self.body(of: request)))
        let (status, json) = Self.replies[request.url?.path ?? ""] ?? (404, "{}")
        let response = HTTPURLResponse(url: request.url!, statusCode: status, httpVersion: "HTTP/1.1",
                                       headerFields: ["Content-Type": "application/json"])!
        client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
        client?.urlProtocol(self, didLoad: Data(json.utf8))
        client?.urlProtocolDidFinishLoading(self)
    }

    override func stopLoading() {}

    private static func body(of request: URLRequest) -> Data {
        if let body = request.httpBody { return body }
        guard let stream = request.httpBodyStream else { return Data() }
        stream.open()
        defer { stream.close() }
        var data = Data()
        var buffer = [UInt8](repeating: 0, count: 4096)
        while stream.hasBytesAvailable {
            let read = stream.read(&buffer, maxLength: buffer.count)
            guard read > 0 else { break }
            data.append(buffer, count: read)
        }
        return data
    }
}
