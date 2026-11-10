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
    [ProtoMember(2)] public bool LinkEmailed { get; set; }
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
    [ProtoMember(5)] public bool DemoAccount { get; set; }
}

[ProtoContract]
public sealed class PinnedPages
{
    [ProtoMember(1)] public List<string> Hrefs { get; set; } = [];
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

[ProtoContract]
public sealed class SearchPeopleRequest
{
    [ProtoMember(1)] public string Query { get; set; } = "";
}

[ProtoContract]
public sealed class GetPeopleRequest
{
    [ProtoMember(1)] public List<string> Ids { get; set; } = [];
}

[ProtoContract]
public sealed class PeopleList
{
    [ProtoMember(1)] public List<PersonInfo> People { get; set; } = [];
}

[ProtoContract]
public sealed class PersonInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string DisplayName { get; set; } = "";
    [ProtoMember(3)] public string Email { get; set; } = "";
    [ProtoMember(4)] public bool EmailConfirmed { get; set; }
    [ProtoMember(5)] public bool Disabled { get; set; }
    [ProtoMember(6)] public bool HasPasskey { get; set; }
    [ProtoMember(7)] public DateTime CreatedAt { get; set; }
    [ProtoMember(8)] public bool Deleted { get; set; }
    [ProtoMember(9)] public bool AddedByFamily { get; set; }
}

[ProtoContract]
public sealed class SetDisabledRequest
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
    [ProtoMember(2)] public bool Disabled { get; set; }
}

[ProtoContract]
public sealed class AuditQuery
{
    [ProtoMember(1)] public string? UserId { get; set; }
}

[ProtoContract]
public sealed class AuditList
{
    [ProtoMember(1)] public List<AuditInfo> Entries { get; set; } = [];
}

[ProtoContract]
public sealed class AuditInfo
{
    [ProtoMember(1)] public DateTime At { get; set; }
    [ProtoMember(2)] public string Action { get; set; } = "";
    [ProtoMember(3)] public string? UserId { get; set; }
    [ProtoMember(4)] public string? ActorId { get; set; }
    [ProtoMember(5)] public string? Detail { get; set; }
}

[ProtoContract]
public sealed class SignInWithCodeRequest
{
    [ProtoMember(1)] public string Email { get; set; } = "";
    [ProtoMember(2)] public string Code { get; set; } = "";
    [ProtoMember(3)] public string? Device { get; set; }
}

[ProtoContract]
public sealed class CodeSession
{
    [ProtoMember(1)] public string Session { get; set; } = "";
}

[ProtoContract]
public sealed class ReviewAccess
{
    [ProtoMember(1)] public List<DemoAccountInfo> DemoAccounts { get; set; } = [];
    [ProtoMember(2)] public List<ReviewCodeInfo> Codes { get; set; } = [];
}

[ProtoContract]
public sealed class DemoAccountInfo
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
    [ProtoMember(2)] public string DisplayName { get; set; } = "";
    [ProtoMember(3)] public string Email { get; set; } = "";
    [ProtoMember(4)] public string? Problem { get; set; }
}

[ProtoContract]
public sealed class ReviewCodeInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string UserId { get; set; } = "";
    [ProtoMember(3)] public string Label { get; set; } = "";
    [ProtoMember(4)] public string CreatedBy { get; set; } = "";
    [ProtoMember(5)] public DateTime CreatedAt { get; set; }
    [ProtoMember(6)] public DateTime ExpiresAt { get; set; }
    [ProtoMember(7)] public DateTime? TurnedOffAt { get; set; }
    [ProtoMember(8)] public int Uses { get; set; }
    [ProtoMember(9)] public DateTime? LastUsedAt { get; set; }
}

[ProtoContract]
public sealed class CreateDemoAccountRequest
{
    [ProtoMember(1)] public string DisplayName { get; set; } = "";
    [ProtoMember(2)] public string Email { get; set; } = "";
}

[ProtoContract]
public sealed class CreateReviewCodeRequest
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
    [ProtoMember(2)] public string Label { get; set; } = "";
    [ProtoMember(3)] public int Days { get; set; }
}

[ProtoContract]
public sealed class TurnOffReviewCodeRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class ConnectToolRequest
{
    [ProtoMember(1)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class ToolConnection
{
    [ProtoMember(1)] public string Session { get; set; } = "";
}

[ProtoContract]
public sealed class InvitationList
{
    [ProtoMember(1)] public List<InvitationInfo> Invitations { get; set; } = [];
}

[ProtoContract]
public sealed class InvitationInfo
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
    [ProtoMember(2)] public string DisplayName { get; set; } = "";
    [ProtoMember(3)] public string Email { get; set; } = "";
    [ProtoMember(4)] public string? InvitedBy { get; set; }
    [ProtoMember(5)] public DateTime InvitedAt { get; set; }
    [ProtoMember(6)] public DateTime ExpiresAt { get; set; }
    [ProtoMember(7)] public bool Held { get; set; }
}

[ProtoContract]
public sealed class AddedPerson
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
}

[ProtoContract]
public sealed class SendHeldInvitationsRequest
{
    [ProtoMember(1)] public int Expected { get; set; }
}

[ProtoContract]
public sealed class SentInvitations
{
    [ProtoMember(1)] public int Sent { get; set; }
}

[ProtoContract]
public sealed class InvitationRequest
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
}

[ProtoContract]
public sealed class MyAccount
{
    [ProtoMember(1, IsRequired = true)] public Profile Profile { get; set; } = new();
    [ProtoMember(2, IsRequired = true)] public PasskeyList Passkeys { get; set; } = new();
    [ProtoMember(3, IsRequired = true)] public SessionList Sessions { get; set; } = new();
}
