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
    private static readonly string[] InvitingActions = [AuditActions.Invited, AuditActions.InvitationResent];

    public async Task SendAsync(AccountUser user, Guid actorId, string action, CancellationToken cancellationToken)
    {
        await OpenCodes(user.Id).ExecuteDeleteAsync(cancellationToken);
        string link = $"{options.Value.SiteUrl.TrimEnd('/')}/welcome?code={codes.IssueToken(user.Id, CodePurpose.Invitation, OneTimeCodes.InvitationLifetime)}";
        audit.Record(action, user.Id, actorId);
        await db.SaveChangesAsync(cancellationToken);
        await email.SendAsync(AccountEmails.Invite(user.Email!, user.DisplayName, link), cancellationToken);
    }

    public async Task<InvitationList> ListAsync(CancellationToken cancellationToken)
    {
        var open = await db.Codes.AsNoTracking()
            .Where(code => code.Purpose == CodePurpose.Invitation && code.UsedAt == null)
            .Join(db.Users.AsNoTracking().Where(user => !user.Disabled && !db.Set<IdentityUserPasskey<Guid>>().Any(passkey => passkey.UserId == user.Id)),
                code => code.UserId, user => user.Id, (code, user) => new { Code = code, User = user })
            .ToListAsync(cancellationToken);
        var latest = open.GroupBy(row => row.User.Id).Select(group => group.MaxBy(row => row.Code.CreatedAt)!).ToList();

        List<Guid> invitees = latest.Select(row => row.User.Id).ToList();
        Dictionary<Guid, Guid?> inviters = (await db.Audit.AsNoTracking()
                .Where(entry => entry.UserId != null && invitees.Contains(entry.UserId.Value) && InvitingActions.Contains(entry.Action))
                .Select(entry => new { UserId = entry.UserId!.Value, entry.ActorId, entry.Id })
                .ToListAsync(cancellationToken))
            .GroupBy(entry => entry.UserId)
            .ToDictionary(group => group.Key, group => group.MaxBy(entry => entry.Id)!.ActorId);
        List<Guid> actorIds = inviters.Values.OfType<Guid>().Distinct().ToList();
        Dictionary<Guid, string> names = await db.Users.AsNoTracking().Where(user => actorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        return new InvitationList
        {
            Invitations = latest.OrderByDescending(row => row.Code.CreatedAt).Select(row => new InvitationInfo
            {
                UserId = row.User.Id.ToString(),
                DisplayName = row.User.DisplayName,
                Email = row.User.Email ?? "",
                InvitedBy = inviters.GetValueOrDefault(row.User.Id) is { } actor ? names.GetValueOrDefault(actor) : null,
                InvitedAt = row.Code.CreatedAt,
                ExpiresAt = row.Code.ExpiresAt,
            }).ToList(),
        };
    }

    public async Task ResendAsync(string userId, Guid actorId, CancellationToken cancellationToken) =>
        await SendAsync(await InviteeAsync(userId, cancellationToken), actorId, AuditActions.InvitationResent, cancellationToken);

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
        if (user is null || user.Disabled || !await OpenCodes(user.Id).AnyAsync(cancellationToken)
            || (await users.GetPasskeysAsync(user)).Count > 0 || await demo.IsDemoAsync(user.Id, cancellationToken))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, NotOpen));
        return user;
    }

    private IQueryable<OneTimeCode> OpenCodes(Guid userId) =>
        db.Codes.Where(code => code.UserId == userId && code.Purpose == CodePurpose.Invitation && code.UsedAt == null);
}
