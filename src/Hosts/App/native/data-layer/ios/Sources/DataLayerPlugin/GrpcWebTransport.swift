import Foundation

struct GrpcResult {
    let status: Int
    let message: String
    let body: Data
}

struct GrpcWebTransport {
    private static let contentType = "application/grpc-web+proto"
    private static let trailerFlag: UInt8 = 0x80
    private static let unknown = 2
    private static let unavailable = 14

    let gateway: URL

    func unary(method: String, message: Data) async throws -> GrpcResult {
        guard let url = URL(string: gateway.absoluteString + method) else {
            return GrpcResult(status: Self.unavailable, message: "Bad method path \(method)", body: Data())
        }

        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue(Self.contentType, forHTTPHeaderField: "Content-Type")
        request.setValue(Self.contentType, forHTTPHeaderField: "Accept")
        request.setValue("1", forHTTPHeaderField: "X-Grpc-Web")
        request.httpBody = Self.frame(message)

        let (data, response) = try await URLSession.shared.data(for: request)
        guard let http = response as? HTTPURLResponse, http.statusCode == 200 else {
            let code = (response as? HTTPURLResponse)?.statusCode ?? 0
            return GrpcResult(status: Self.unavailable, message: "HTTP \(code) from \(url.host ?? "the gateway")", body: Data())
        }

        var trailers = Self.headers(http)
        var body = Data()
        var offset = data.startIndex
        while data.endIndex - offset >= 5 {
            let flag = data[offset]
            let length = data[(offset + 1)..<(offset + 5)].reduce(0) { $0 << 8 | Int($1) }
            let start = offset + 5
            guard data.endIndex - start >= length else { break }
            let payload = data[start..<(start + length)]
            if flag & Self.trailerFlag != 0 {
                trailers.merge(Self.parseTrailers(payload)) { _, new in new }
            } else {
                body.append(payload)
            }
            offset = start + length
        }

        let status = trailers["grpc-status"].flatMap { Int($0) } ?? Self.unknown
        let message = trailers["grpc-message"]?.removingPercentEncoding ?? ""
        return GrpcResult(status: status, message: message, body: body)
    }

    private static func frame(_ message: Data) -> Data {
        var framed = Data([0])
        withUnsafeBytes(of: UInt32(message.count).bigEndian) { framed.append(contentsOf: $0) }
        framed.append(message)
        return framed
    }

    private static func headers(_ response: HTTPURLResponse) -> [String: String] {
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
