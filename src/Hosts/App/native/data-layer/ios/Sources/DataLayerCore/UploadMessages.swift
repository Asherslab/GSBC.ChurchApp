import Foundation

public struct UploadTicket: Equatable {
    public let uploadId: String
    public let url: URL
    public let contentType: String
}

public enum UploadMessages {
    static let mediaIdPath = [1, 1, 1]
    static let uploadIdPath = [1]

    public static func mediaId(in reply: Data) throws -> String? {
        try firstString(at: mediaIdPath, in: reply).flatMap { $0.isEmpty ? nil : $0 }
    }

    public static func ticket(from reply: Data) throws -> UploadTicket {
        var reader = ProtoReader(reply)
        var uploadId = "", url = "", contentType = ""
        while !reader.isAtEnd {
            let (field, wireType) = try reader.nextField()
            switch (field, wireType) {
            case (1, 2): uploadId = try reader.string()
            case (2, 2): url = try reader.string()
            case (3, 2): contentType = try reader.string()
            default: try reader.skip(wireType: wireType)
            }
        }
        guard !uploadId.isEmpty, let parsed = URL(string: url) else { throw ProtoError(reason: "an upload ticket without an id or address") }
        return UploadTicket(uploadId: uploadId, url: parsed, contentType: contentType)
    }

    public static func splice(_ value: String, at path: [Int], into message: Data) -> Data {
        guard let innermost = path.last else { return message }
        var inner = ProtoWriter()
        inner.string(innermost, value)
        var wrapped = inner.data
        for field in path.dropLast().reversed() {
            var outer = ProtoWriter()
            outer.bytes(field, wrapped)
            wrapped = outer.data
        }
        return message + wrapped
    }

    public static func path(_ text: String) -> [Int]? {
        let fields = text.split(separator: ".").map { Int($0) }
        guard !fields.isEmpty, fields.allSatisfy({ ($0 ?? 0) > 0 }) else { return nil }
        return fields.compactMap { $0 }
    }

    private static func firstString(at path: [Int], in message: Data) throws -> String? {
        var current = message
        for (depth, wanted) in path.enumerated() {
            var reader = ProtoReader(current)
            var found: Data?
            while !reader.isAtEnd, found == nil {
                let (field, wireType) = try reader.nextField()
                if field == wanted && wireType == 2 {
                    found = try reader.bytes()
                } else {
                    try reader.skip(wireType: wireType)
                }
            }
            guard let found else { return nil }
            if depth == path.count - 1 { return String(decoding: found, as: UTF8.self) }
            current = found
        }
        return nil
    }
}
