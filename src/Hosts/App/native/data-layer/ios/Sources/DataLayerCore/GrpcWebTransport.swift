import Foundation

public struct GrpcResult {
    public let status: Int
    public let message: String
    public let body: Data

    public init(status: Int, message: String, body: Data) {
        self.status = status
        self.message = message
        self.body = body
    }
}

public struct GrpcError: Error {
    public let status: Int
    public let message: String
}

public protocol RequestSigner: Sendable {
    func headers(method: String, body: Data) async -> [String: String]
}

public struct GrpcWebTransport: Sendable {
    public init(gateway: URL, sessionSecret: @escaping @Sendable () -> String? = { nil },
                onSessionEnded: @escaping @Sendable () -> Void = {}, session: URLSession = .shared, signer: RequestSigner? = nil) {
        self.gateway = gateway
        self.session = session
        self.sessionSecret = sessionSecret
        self.onSessionEnded = onSessionEnded
        self.signer = signer
    }

    public static let ok = 0
    public static let unknown = 2
    public static let unimplemented = 12
    public static let unavailable = 14
    private static let httpNotFound = 404

    private static let contentType = "application/grpc-web+proto"
    private static let trailerFlag: UInt8 = 0x80
    private static let frameHeaderLength = 5
    private static let streamIdleTimeout: TimeInterval = 90

    public let gateway: URL
    private let session: URLSession
    private let sessionSecret: @Sendable () -> String?
    private let onSessionEnded: @Sendable () -> Void
    private let signer: RequestSigner?
    private static let unsignedPrefixes = ["/gsbc.sync.Sync/", InstallMessages.beginMethod, InstallMessages.attestMethod]
    public static let sessionHeader = "X-Gsbc-Session"
    public static let sessionEndedHeader = "X-Gsbc-Session-Ended"

    public func unary(method: String, message: Data, headers: [String: String] = [:]) async throws -> GrpcResult {
        let framed = Self.frame(message)
        var headers = headers
        if let signer, !Self.unsignedPrefixes.contains(where: method.hasPrefix) {
            headers.merge(await signer.headers(method: method, body: framed)) { mine, _ in mine }
        }
        let (data, response) = try await session.data(for: try request(method, framed, headers))
        noticeEndedSession(response)
        let http = try Self.successful(response)

        var trailers = Self.grpcHeaders(http)
        var body = Data()
        var buffer = data
        while let frame = Self.takeFrame(&buffer) {
            if frame.isTrailers {
                trailers.merge(Self.parseTrailers(frame.payload)) { _, new in new }
            } else {
                body.append(frame.payload)
            }
        }
        let (status, message) = Self.status(trailers)
        return GrpcResult(status: status, message: message, body: body)
    }

    public func serverStream(method: String, message: Data) -> AsyncThrowingStream<Data, Error> {
        AsyncThrowingStream { continuation in
            let task = Task {
                do {
                    var request = try request(method, Self.frame(message), [:])
                    request.timeoutInterval = Self.streamIdleTimeout
                    let (bytes, response) = try await session.bytes(for: request)
                    noticeEndedSession(response)
                    let http = try Self.successful(response)

                    var trailers = Self.grpcHeaders(http)
                    var buffer = Data()
                    for try await byte in bytes {
                        buffer.append(byte)
                        while let frame = Self.takeFrame(&buffer) {
                            if frame.isTrailers {
                                trailers.merge(Self.parseTrailers(frame.payload)) { _, new in new }
                            } else {
                                continuation.yield(frame.payload)
                            }
                        }
                    }

                    let (status, message) = Self.status(trailers)
                    if status != Self.ok { throw GrpcError(status: status, message: message) }
                    continuation.finish()
                } catch {
                    continuation.finish(throwing: error)
                }
            }
            continuation.onTermination = { _ in task.cancel() }
        }
    }

    private func request(_ method: String, _ body: Data, _ headers: [String: String]) throws -> URLRequest {
        guard let url = URL(string: gateway.absoluteString + method) else {
            throw GrpcError(status: Self.unavailable, message: "Bad method path \(method)")
        }
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue(Self.contentType, forHTTPHeaderField: "Content-Type")
        request.setValue(Self.contentType, forHTTPHeaderField: "Accept")
        request.setValue("1", forHTTPHeaderField: "X-Grpc-Web")
        if let secret = sessionSecret() { request.setValue(secret, forHTTPHeaderField: Self.sessionHeader) }
        for (name, value) in headers { request.setValue(value, forHTTPHeaderField: name) }
        request.httpBody = body
        return request
    }

    private func noticeEndedSession(_ response: URLResponse) {
        if (response as? HTTPURLResponse)?.value(forHTTPHeaderField: Self.sessionEndedHeader) != nil { onSessionEnded() }
    }

    private static func successful(_ response: URLResponse) throws -> HTTPURLResponse {
        guard let http = response as? HTTPURLResponse, http.statusCode == 200 else {
            let code = (response as? HTTPURLResponse)?.statusCode ?? 0
            throw GrpcError(status: code == httpNotFound ? unimplemented : unavailable,
                            message: "HTTP \(code) from \(response.url?.host ?? "the gateway")")
        }
        let (status, message) = status(grpcHeaders(http), missing: ok)
        if status != ok { throw GrpcError(status: status, message: message) }
        return http
    }

    private static func frame(_ message: Data) -> Data {
        var framed = Data([0])
        withUnsafeBytes(of: UInt32(message.count).bigEndian) { framed.append(contentsOf: $0) }
        framed.append(message)
        return framed
    }

    private static func takeFrame(_ buffer: inout Data) -> (isTrailers: Bool, payload: Data)? {
        guard buffer.count >= frameHeaderLength else { return nil }
        let start = buffer.startIndex
        let length = buffer[(start + 1)..<(start + frameHeaderLength)].reduce(0) { $0 << 8 | Int($1) }
        guard buffer.count >= frameHeaderLength + length else { return nil }
        let isTrailers = buffer[start] & trailerFlag != 0
        let payload = Data(buffer[(start + frameHeaderLength)..<(start + frameHeaderLength + length)])
        buffer = Data(buffer[(start + frameHeaderLength + length)...])
        return (isTrailers, payload)
    }

    private static func status(_ trailers: [String: String], missing: Int = unknown) -> (Int, String) {
        let status = trailers["grpc-status"].flatMap { Int($0) } ?? missing
        return (status, trailers["grpc-message"]?.removingPercentEncoding ?? "")
    }

    private static func grpcHeaders(_ response: HTTPURLResponse) -> [String: String] {
        var result: [String: String] = [:]
        for (key, value) in response.allHeaderFields {
            if let key = key as? String, let value = value as? String, key.lowercased().hasPrefix("grpc-") {
                result[key.lowercased()] = value
            }
        }
        return result
    }

    private static func parseTrailers(_ payload: Data) -> [String: String] {
        var result: [String: String] = [:]
        for line in String(decoding: payload, as: UTF8.self).split(whereSeparator: \.isNewline) {
            let parts = line.split(separator: ":", maxSplits: 1)
            if parts.count == 2 {
                result[parts[0].trimmingCharacters(in: .whitespaces).lowercased()] = parts[1].trimmingCharacters(in: .whitespaces)
            }
        }
        return result
    }
}
