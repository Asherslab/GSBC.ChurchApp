import Foundation

struct ProtoWriter {
    private(set) var data = Data()

    mutating func string(_ field: Int, _ value: String) {
        bytes(field, Data(value.utf8))
    }

    mutating func bytes(_ field: Int, _ value: Data) {
        tag(field, wireType: 2)
        varint(UInt64(value.count))
        data.append(value)
    }

    mutating func int64(_ field: Int, _ value: Int64) {
        guard value != 0 else { return }
        tag(field, wireType: 0)
        varint(UInt64(bitPattern: value))
    }

    private mutating func tag(_ field: Int, wireType: UInt64) {
        varint(UInt64(field) << 3 | wireType)
    }

    private mutating func varint(_ value: UInt64) {
        var remaining = value
        while remaining >= 0x80 {
            data.append(UInt8(remaining & 0x7F) | 0x80)
            remaining >>= 7
        }
        data.append(UInt8(remaining))
    }
}

struct ProtoError: Error {
    let reason: String
}

struct ProtoReader {
    private let data: Data
    private var offset: Data.Index

    init(_ data: Data) {
        self.data = data
        self.offset = data.startIndex
    }

    var isAtEnd: Bool { offset >= data.endIndex }

    mutating func nextField() throws -> (field: Int, wireType: Int) {
        let tag = try varint()
        return (Int(tag >> 3), Int(tag & 0x7))
    }

    mutating func varint() throws -> UInt64 {
        var result: UInt64 = 0
        var shift: UInt64 = 0
        while true {
            guard offset < data.endIndex, shift < 64 else { throw ProtoError(reason: "truncated varint") }
            let byte = data[offset]
            offset += 1
            result |= UInt64(byte & 0x7F) << shift
            if byte & 0x80 == 0 { return result }
            shift += 7
        }
    }

    mutating func int64() throws -> Int64 { Int64(bitPattern: try varint()) }

    mutating func bool() throws -> Bool { try varint() != 0 }

    mutating func bytes() throws -> Data {
        let length = Int(try varint())
        guard data.endIndex - offset >= length else { throw ProtoError(reason: "truncated field") }
        let value = data[offset..<(offset + length)]
        offset += length
        return Data(value)
    }

    mutating func string() throws -> String { String(decoding: try bytes(), as: UTF8.self) }

    mutating func skip(wireType: Int) throws {
        switch wireType {
        case 0: _ = try varint()
        case 1: offset += 8
        case 2: _ = try bytes()
        case 5: offset += 4
        default: throw ProtoError(reason: "unsupported wire type \(wireType)")
        }
        guard offset <= data.endIndex else { throw ProtoError(reason: "truncated field") }
    }
}
