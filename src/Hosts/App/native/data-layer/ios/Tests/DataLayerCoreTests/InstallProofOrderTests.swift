import DataLayerCore
import Foundation
import XCTest

final class InstallProofOrderTests: XCTestCase {
    private static let registerDevice = "/gsbc.notifications.Notifications/RegisterDevice"
    private static let setPhonePreference = "/gsbc.notifications.Notifications/SetPhonePreference"
    private static let getProfile = "/gsbc.accounts.Accounts/GetProfile"

    override func setUp() {
        StrictCounterGateway.reset(proofMethods: [Self.registerDevice, Self.setPhonePreference])
    }

    func testOverlappingSignedCallsBothGetThroughWhenTheFirstIsSlowerOnTheWire() async throws {
        StrictCounterGateway.delayFirstSigned = 0.3
        let transport = transport()

        async let first = transport.unary(method: Self.registerDevice, message: Data([1]))
        async let second = transport.unary(method: Self.setPhonePreference, message: Data([2]))
        let statuses = try await [first.status, second.status]

        XCTAssertEqual(statuses, [GrpcWebTransport.ok, GrpcWebTransport.ok])
    }

    func testOnlyTheMethodsTheServerListsAreSigned() async throws {
        let transport = transport()

        _ = try await transport.unary(method: Self.getProfile, message: Data())
        _ = try await transport.unary(method: Self.registerDevice, message: Data())

        XCTAssertEqual(StrictCounterGateway.signedPaths, [Self.registerDevice])
    }

    func testAMethodThatStartsNeedingProofWhileTheAppRunsIsSignedOnARetry() async throws {
        let transport = transport()
        _ = try await transport.unary(method: Self.registerDevice, message: Data())

        StrictCounterGateway.reset(proofMethods: [Self.registerDevice, Self.getProfile])
        let result = try await transport.unary(method: Self.getProfile, message: Data())

        XCTAssertEqual(result.status, GrpcWebTransport.ok)
        XCTAssertEqual(StrictCounterGateway.signedPaths, [Self.getProfile])
    }

    private func transport() -> GrpcWebTransport {
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [StrictCounterGateway.self]
        return GrpcWebTransport(gateway: URL(string: "https://dev-app.baptist.com.au")!,
                                session: URLSession(configuration: configuration), signer: CountingSigner())
    }
}

private actor CountingSigner: RequestSigner {
    private var counter = 0

    func canSign() async -> Bool { true }

    func headers(method: String, body: Data) async -> [String: String] {
        counter += 1
        return ["gsbc-install": "a-phone", "gsbc-assertion": String(counter)]
    }
}

private final class StrictCounterGateway: URLProtocol {
    private static let listMethod = "/gsbc.accounts.Installs/ListProofMethods"
    private static let lock = NSLock()
    nonisolated(unsafe) private static var storedCounter = 0
    nonisolated(unsafe) private static var proofMethods: [String] = []
    nonisolated(unsafe) private static var signed: [String] = []
    nonisolated(unsafe) static var delayFirstSigned: TimeInterval = 0

    static var signedPaths: [String] { lock.withLock { signed } }

    static func reset(proofMethods: [String]) {
        lock.withLock {
            storedCounter = 0
            delayFirstSigned = 0
            signed = []
            self.proofMethods = proofMethods
        }
    }

    override class func canInit(with request: URLRequest) -> Bool { true }
    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        let isFirstSigned = request.value(forHTTPHeaderField: "gsbc-assertion") == "1"
        let delay = isFirstSigned ? Self.lock.withLock { Self.delayFirstSigned } : 0
        DispatchQueue.global().asyncAfter(deadline: .now() + delay) { self.answer() }
    }

    private func answer() {
        let path = request.url!.path
        let counter = request.value(forHTTPHeaderField: "gsbc-assertion").flatMap { Int($0) }
        var headers = ["Content-Type": "application/grpc-web+proto", "grpc-status": "0"]
        var body = Data()
        Self.lock.withLock {
            if counter != nil { Self.signed.append(path) }
            if path == Self.listMethod {
                var list = ProtoWriter()
                for method in Self.proofMethods { list.string(1, method) }
                body = Self.frame(list.data)
            } else if Self.proofMethods.contains(path) {
                if let counter, counter > Self.storedCounter {
                    Self.storedCounter = counter
                } else {
                    headers["grpc-status"] = "16"
                    if counter == nil { headers["gsbc-install-proof"] = "required" }
                }
            }
        }
        let response = HTTPURLResponse(url: request.url!, statusCode: 200, httpVersion: "HTTP/1.1", headerFields: headers)!
        client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
        client?.urlProtocol(self, didLoad: body)
        client?.urlProtocolDidFinishLoading(self)
    }

    private static func frame(_ payload: Data) -> Data {
        var framed = Data([0])
        withUnsafeBytes(of: UInt32(payload.count).bigEndian) { framed.append(contentsOf: $0) }
        return framed + payload
    }

    override func stopLoading() {}
}
