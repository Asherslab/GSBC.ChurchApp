using ProtoBuf;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

[ProtoContract]
public sealed class GetCurrentSessionRequest;

[ProtoContract]
public sealed class CurrentSession
{
    [ProtoMember(1)] public bool SignedIn { get; set; }
    [ProtoMember(2)] public string? UserId { get; set; }
    [ProtoMember(3)] public string? DisplayName { get; set; }
}
