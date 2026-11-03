import DataLayerCore
import Foundation
import XCTest

final class CodeSignInTests: XCTestCase {
    private static let session = "code-session"

    override func tearDown() {
        StubGrpcGateway.reply = Data()
        StubGrpcGateway.headers = [:]
    }

    func testSigningInWithACodeKeepsTheSessionInTheVaultAndHandsBackOnlyTheOutcome() async {
        var reply = ProtoWriter()
        reply.string(1, Self.session)
        StubGrpcGateway.reply = Self.frame(reply.data) + Self.trailers("grpc-status: 0")
        let vault = MemoryVault()

        let outcome = await signIn(vault).signIn(email: "review@example.com", code: "ACDE-FGHJ-KMNP")

        XCTAssertEqual(outcome, .succeeded)
        XCTAssertEqual(vault.read(), Self.session)
        XCTAssertFalse(String(describing: outcome).contains(Self.session))
    }

    func testAWrongCodeIsShownInTheServersWordsAndLeavesThePhoneSignedOut() async {
        StubGrpcGateway.headers = ["grpc-status": "16", "grpc-message": "That%20email%20and%20sign-in%20code%20don%27t%20match."]
        let vault = MemoryVault()

        let outcome = await signIn(vault).signIn(email: "review@example.com", code: "AAAA-AAAA-AAAA")

        XCTAssertEqual(outcome, .failed("That email and sign-in code don't match."))
        XCTAssertNil(vault.read())
    }

    private func signIn(_ vault: MemoryVault) -> CodeSignIn {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [StubGrpcGateway.self]
        let transport = GrpcWebTransport(gateway: URL(string: "https://dev-app.baptist.com.au")!, session: URLSession(configuration: configuration))
        return CodeSignIn(transport: transport, vault: vault, deviceName: "iPhone")
    }

    private static func frame(_ payload: Data, flag: UInt8 = 0) -> Data {
        var framed = Data([flag])
        withUnsafeBytes(of: UInt32(payload.count).bigEndian) { framed.append(contentsOf: $0) }
        return framed + payload
    }

    private static func trailers(_ text: String) -> Data { frame(Data(text.utf8), flag: 0x80) }
}

private final class MemoryVault: SessionVault, @unchecked Sendable {
    private var secret: String?
    func read() -> String? { secret }
    func write(_ secret: String?) { self.secret = secret }
}

private final class StubGrpcGateway: URLProtocol {
    nonisolated(unsafe) static var reply = Data()
    nonisolated(unsafe) static var headers: [String: String] = [:]

    override class func canInit(with request: URLRequest) -> Bool { true }
    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        let response = HTTPURLResponse(url: request.url!, statusCode: 200, httpVersion: "HTTP/1.1",
                                       headerFields: ["Content-Type": "application/grpc-web+proto"].merging(Self.headers) { _, new in new })!
        client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
        client?.urlProtocol(self, didLoad: Self.reply)
        client?.urlProtocolDidFinishLoading(self)
    }

    override func stopLoading() {}
}
