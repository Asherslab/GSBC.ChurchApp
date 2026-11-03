import Foundation

public enum InstallMessages {
    public static let beginMethod = "/gsbc.accounts.Installs/BeginAttestation"
    public static let attestMethod = "/gsbc.accounts.Installs/Attest"

    public static func challenge(from reply: Data) throws -> Data {
        var reader = ProtoReader(reply)
        while !reader.isAtEnd {
            let (field, wireType) = try reader.nextField()
            if field == 1 && wireType == 2 { return try reader.bytes() }
            try reader.skip(wireType: wireType)
        }
        throw ProtoError(reason: "no challenge")
    }

    public static func attestRequest(keyId: String, challenge: Data, attestation: Data) -> Data {
        var writer = ProtoWriter()
        writer.string(1, keyId)
        writer.bytes(2, challenge)
        writer.bytes(3, attestation)
        return writer.data
    }

    public static func clientData(method: String, body: Data) -> Data {
        Data((method + "\n").utf8) + body
    }
}
