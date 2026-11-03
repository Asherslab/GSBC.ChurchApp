using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AccountsAudit(AccountsDbContext db, TimeProvider time)
{
    public void Record(string action, Guid? userId, Guid? actorId = null, string? detail = null, string? remoteAddress = null)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        db.Audit.Add(new AuditEntry
        {
            At = now,
            Action = action,
            UserId = userId,
            ActorId = actorId,
            Detail = detail,
            RemoteAddress = remoteAddress,
        });
        if (userId is { } person)
            db.FeedChanges.Add(FeedChangeRecord.Marker(AccountsFeeds.People, person.ToString(), now));
    }
}

public static class AuditActions
{
    public const string SignedUp = "signed-up";
    public const string SignedIn = "signed-in";
    public const string PasskeyAdded = "passkey-added";
    public const string PasskeyRemoved = "passkey-removed";
    public const string EmailConfirmed = "email-confirmed";
    public const string RecoveryRequested = "recovery-requested";
    public const string RecoveryIssued = "recovery-issued";
    public const string CodeRedeemed = "code-redeemed";
    public const string Invited = "invited";
    public const string InvitationResent = "invitation-resent";
    public const string InvitationWithdrawn = "invitation-withdrawn";
    public const string SessionEnded = "session-ended";
    public const string SignedOutEverywhere = "signed-out-everywhere";
    public const string NameChanged = "name-changed";
    public const string Disabled = "disabled";
    public const string Enabled = "enabled";
    public const string DemoAccountCreated = "demo-account-created";
    public const string ReviewCodeCreated = "review-code-created";
    public const string ReviewCodeTurnedOff = "review-code-turned-off";
    public const string ReviewCodeUsed = "review-code-used";
    public const string ReviewCodeRefused = "review-code-refused";
    public const string ToolConnected = "tool-connected";
}
