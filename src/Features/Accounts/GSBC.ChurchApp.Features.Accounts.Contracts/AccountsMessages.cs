using ProtoBuf;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

[ProtoContract]
public sealed class Nothing;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class StartSignUpRequest
{
    [ProtoMember(1)] public string DisplayName { get; set; } = "";
    [ProtoMember(2)] public string Email { get; set; } = "";
}

[ProtoContract]
public sealed class TicketReply
{
    [ProtoMember(1)] public string Ticket { get; set; } = "";
}

[ProtoContract]
public sealed class RedeemRequest
{
    [ProtoMember(1)] public string Code { get; set; } = "";
}

[ProtoContract]
public sealed class RequestRecoveryRequest
{
    [ProtoMember(1)] public string Email { get; set; } = "";
}

[ProtoContract]
public sealed class Profile
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
    [ProtoMember(2)] public string DisplayName { get; set; } = "";
    [ProtoMember(3)] public string Email { get; set; } = "";
    [ProtoMember(4)] public bool EmailConfirmed { get; set; }
}

[ProtoContract]
public sealed class UpdateNameRequest
{
    [ProtoMember(1)] public string DisplayName { get; set; } = "";
}

[ProtoContract]
public sealed class ConfirmEmailRequest
{
    [ProtoMember(1)] public string Code { get; set; } = "";
}

[ProtoContract]
public sealed class PasskeyList
{
    [ProtoMember(1)] public List<PasskeySummary> Passkeys { get; set; } = [];
}

[ProtoContract]
public sealed class PasskeySummary
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public DateTime CreatedAt { get; set; }
    [ProtoMember(4)] public bool BackedUp { get; set; }
}

[ProtoContract]
public sealed class RemovePasskeyRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class SessionList
{
    [ProtoMember(1)] public List<SessionSummary> Sessions { get; set; } = [];
}

[ProtoContract]
public sealed class SessionSummary
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Client { get; set; } = "";
    [ProtoMember(3)] public string? DeviceName { get; set; }
    [ProtoMember(4)] public DateTime CreatedAt { get; set; }
    [ProtoMember(5)] public DateTime LastSeenAt { get; set; }
    [ProtoMember(6)] public bool Current { get; set; }
}

[ProtoContract]
public sealed class EndSessionRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class InviteRequest
{
    [ProtoMember(1)] public string DisplayName { get; set; } = "";
    [ProtoMember(2)] public string Email { get; set; } = "";
}

[ProtoContract]
public sealed class IssueRecoveryCodeRequest
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
}

[ProtoContract]
public sealed class CodeReply
{
    [ProtoMember(1)] public string Code { get; set; } = "";
    [ProtoMember(2)] public DateTime ExpiresAt { get; set; }
}
