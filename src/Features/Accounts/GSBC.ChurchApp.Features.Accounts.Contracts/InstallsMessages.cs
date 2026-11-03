using ProtoBuf;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

[ProtoContract]
public sealed class BeginAttestationRequest;

[ProtoContract]
public sealed class AttestationChallenge
{
    [ProtoMember(1)] public byte[] Challenge { get; set; } = [];
}

[ProtoContract]
public sealed class ProofMethods
{
    [ProtoMember(1)] public List<string> Paths { get; set; } = [];
}

[ProtoContract]
public sealed class AttestRequest
{
    [ProtoMember(1)] public string KeyId { get; set; } = "";
    [ProtoMember(2)] public byte[] Challenge { get; set; } = [];
    [ProtoMember(3)] public byte[] Attestation { get; set; } = [];
}
