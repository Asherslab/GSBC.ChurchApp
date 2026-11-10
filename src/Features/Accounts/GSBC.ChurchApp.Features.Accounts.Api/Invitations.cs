using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class Invitations(
    AccountsDbContext db,
    UserManager<AccountUser> users,
    OneTimeCodes codes,
    AccountsAudit audit,
    IEmailSender email,
    IOptions<AccountsOptions> options,
    DemoAccounts demo,
    AccountDeletion deletion)
{
    private const string NotOpen = "That invitation was already accepted or withdrawn.";
    private const string AlreadyWaiting = "They're already on the list. Find them on People to send or withdraw their invitation.";
    private const string ListChanged = "The list changed while you were looking. Check it again before sending.";
    private static readonly IReadOnlyList<string> InvitingActions = [AuditActions.Invited, AuditActions.InvitationResent];
    private static readonly IReadOnlyList<string> AddingActions = [AuditActions.AddedWithoutInvitation];

    public async Task HoldAsync(AccountUser user, Guid actorId, CancellationToken cancellationToken)
    {
        if (await AwaitsInvitationAsync(user, cancellationToken))
            throw new RpcException(new Status(StatusCode.AlreadyExists, AlreadyWaiting));
        user.InvitationHeld = true;
        audit.Record(AuditActions.AddedWithoutInvitation, user.Id, actorId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AwaitsInvitationAsync(AccountUser user, CancellationToken cancellationToken) =>
        user.InvitationHeld || await OpenCodes(user.Id).AnyAsync(cancellationToken);

    public async Task<int> SendHeldAsync(int expected, Guid actorId, CancellationToken cancellationToken)
    {
        List<Guid> held = await Held().Select(user => user.Id).ToListAsync(cancellationToken);
        if (held.Count != expected)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ListChanged));

        int sent = 0;
        foreach (Guid userId in held)
        {
            if (await ClaimHeldAsync(userId, actorId, cancellationToken) is not { } invitation)
                continue;
            await email.SendAsync(invitation, cancellationToken);
            sent++;
        }
        return sent;
    }

    public async Task SendAsync(AccountUser user, Guid? actorId, string action, CancellationToken cancellationToken) =>
        await email.SendAsync(await IssueAsync(user, actorId, action, cancellationToken), cancellationToken);

    private async Task<EmailMessage?> ClaimHeldAsync(Guid userId, Guid actorId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (await Held().Where(user => user.Id == userId)
                .ExecuteUpdateAsync(set => set.SetProperty(user => user.InvitationHeld, false), cancellationToken) == 0)
            return null;
        AccountUser user = await db.Users.SingleAsync(candidate => candidate.Id == userId, cancellationToken);
        EmailMessage invitation = await IssueAsync(user, actorId, AuditActions.Invited, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return invitation;
    }

    private async Task<EmailMessage> IssueAsync(AccountUser user, Guid? actorId, string action, CancellationToken cancellationToken)
    {
        user.InvitationHeld = false;
        await OpenCodes(user.Id).ExecuteDeleteAsync(cancellationToken);
        string link = $"{options.Value.SiteUrl.TrimEnd('/')}/welcome?code={codes.IssueToken(user.Id, CodePurpose.Invitation, OneTimeCodes.InvitationLifetime)}";
        audit.Record(action, user.Id, actorId);
        await db.SaveChangesAsync(cancellationToken);
        return AccountEmails.Invite(user.Email!, user.DisplayName, link);
    }

    public async Task<InvitationList> ListAsync(CancellationToken cancellationToken)
    {
        var open = await db.Codes.AsNoTracking()
            .Where(code => code.Purpose == CodePurpose.Invitation && code.UsedAt == null)
            .Join(db.Users.AsNoTracking().Where(user => !user.Disabled && !db.Set<IdentityUserPasskey<Guid>>().Any(passkey => passkey.UserId == user.Id)),
                code => code.UserId, user => user.Id, (code, user) => new { Code = code, User = user })
            .ToListAsync(cancellationToken);
        var latest = open.GroupBy(row => row.User.Id).Select(group => group.MaxBy(row => row.Code.CreatedAt)!).ToList();

        Dictionary<Guid, Guid?> inviters = await LatestActorsAsync(latest.Select(row => row.User.Id).ToList(), InvitingActions, cancellationToken);

        List<AccountUser> held = await Held().AsNoTracking().OrderByDescending(user => user.CreatedAt).ToListAsync(cancellationToken);
        Dictionary<Guid, Guid?> adders = await LatestActorsAsync(held.Select(user => user.Id).ToList(), AddingActions, cancellationToken);
        List<Guid> actorIds = inviters.Values.Concat(adders.Values).OfType<Guid>().Distinct().ToList();
        Dictionary<Guid, string> names = await db.Users.AsNoTracking().Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);
        string? NameOf(Dictionary<Guid, Guid?> actors, Guid userId) => actors.GetValueOrDefault(userId) is { } actor ? names.GetValueOrDefault(actor) : null;

        return new InvitationList
        {
            Invitations =
            [
                .. held.Select(user => new InvitationInfo
                {
                    UserId = user.Id.ToString(),
                    DisplayName = user.DisplayName,
                    Email = user.Email ?? "",
                    InvitedBy = NameOf(adders, user.Id),
                    InvitedAt = user.CreatedAt,
                    Held = true,
                }),
                .. latest.OrderByDescending(row => row.Code.CreatedAt).Select(row => new InvitationInfo
                {
                    UserId = row.User.Id.ToString(),
                    DisplayName = row.User.DisplayName,
                    Email = row.User.Email ?? "",
                    InvitedBy = NameOf(inviters, row.User.Id),
                    InvitedAt = row.Code.CreatedAt,
                    ExpiresAt = row.Code.ExpiresAt,
                }),
            ],
        };
    }

    private async Task<Dictionary<Guid, Guid?>> LatestActorsAsync(List<Guid> userIds, IReadOnlyList<string> actions, CancellationToken cancellationToken) =>
        (await db.Audit.AsNoTracking()
            .Where(entry => entry.UserId != null && userIds.Contains(entry.UserId.Value) && actions.Contains(entry.Action))
            .Select(entry => new { UserId = entry.UserId!.Value, entry.ActorId, entry.Id })
            .ToListAsync(cancellationToken))
        .GroupBy(entry => entry.UserId)
        .ToDictionary(group => group.Key, group => group.MaxBy(entry => entry.Id)!.ActorId);

    public async Task ResendAsync(string userId, Guid actorId, CancellationToken cancellationToken)
    {
        AccountUser user = await InviteeAsync(userId, cancellationToken);
        await SendAsync(user, actorId, user.InvitationHeld ? AuditActions.Invited : AuditActions.InvitationResent, cancellationToken);
    }

    public async Task WithdrawAsync(string userId, Guid actorId, CancellationToken cancellationToken)
    {
        AccountUser user = await InviteeAsync(userId, cancellationToken);
        await db.Codes.Where(code => code.UserId == user.Id && code.UsedAt == null).ExecuteDeleteAsync(cancellationToken);
        audit.Record(AuditActions.InvitationWithdrawn, user.Id, actorId, detail: user.Email);
        await db.SaveChangesAsync(cancellationToken);
        await deletion.ForgetEverywhereAsync(user.Id, cancellationToken);
        IdentityResult deleted = await users.DeleteAsync(user);
        if (!deleted.Succeeded)
            throw new RpcException(new Status(StatusCode.Internal, string.Join(" ", deleted.Errors.Select(error => error.Description))));
    }

    private async Task<AccountUser> InviteeAsync(string userId, CancellationToken cancellationToken)
    {
        AccountUser? user = Guid.TryParse(userId, out Guid id) ? await users.FindByIdAsync(id.ToString()) : null;
        if (user is null || user.Disabled || (!user.InvitationHeld && !await OpenCodes(user.Id).AnyAsync(cancellationToken))
            || (await users.GetPasskeysAsync(user)).Count > 0 || await demo.IsDemoAsync(user.Id, cancellationToken))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, NotOpen));
        return user;
    }

    private IQueryable<AccountUser> Held() =>
        db.Users.Where(user => user.InvitationHeld && !user.Disabled && user.DeletedAt == null
            && !db.Set<IdentityUserPasskey<Guid>>().Any(passkey => passkey.UserId == user.Id));

    private IQueryable<OneTimeCode> OpenCodes(Guid userId) =>
        db.Codes.Where(code => code.UserId == userId && code.Purpose == CodePurpose.Invitation && code.UsedAt == null);
}
