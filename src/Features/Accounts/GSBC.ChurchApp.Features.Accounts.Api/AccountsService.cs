using System.Net.Mail;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class AccountsService(
    AccountsDbContext db,
    UserManager<AccountUser> users,
    OneTimeCodes codes,
    AccountsAudit audit,
    IEmailSender email,
    IOptions<AccountsOptions> options,
    TimeProvider time) : IAccountsService
{
    private const int MaximumNameLength = 80;
    private static readonly CodePurpose[] RedeemablePurposes = [CodePurpose.Invitation, CodePurpose.Recovery];

    [EnableRateLimiting(RateLimits.PublicWrites)]
    public async ValueTask<TicketReply> StartSignUpAsync(StartSignUpRequest request, CallContext context = default)
    {
        string name = ValidName(request.DisplayName);
        string address = ValidEmail(request.Email);
        CancellationToken cancellationToken = context.CancellationToken;

        AccountUser user = await FindAbandonedOrCreateAsync(name, address, cancellationToken);
        string ticket = codes.IssueToken(user.Id, CodePurpose.RegisterPasskey, OneTimeCodes.TicketLifetime);
        string confirmation = await codes.IssueDigitsAsync(user.Id, CodePurpose.ConfirmEmail, OneTimeCodes.ConfirmationLifetime, cancellationToken);
        audit.Record(AuditActions.SignedUp, user.Id, remoteAddress: Address(context));
        await db.SaveChangesAsync(cancellationToken);

        await email.SendAsync(AccountEmails.Confirm(address, name, confirmation), cancellationToken);
        return new TicketReply { Ticket = ticket };
    }

    [EnableRateLimiting(RateLimits.PublicWrites)]
    public async ValueTask<TicketReply> RedeemAsync(RedeemRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        OneTimeCode code = await codes.FindTokenAsync(request.Code.Trim(), RedeemablePurposes, cancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That link has expired or was already used. Ask for a new one."));

        codes.Use(code);
        AccountUser user = await users.FindByIdAsync(code.UserId.ToString())
            ?? throw new RpcException(new Status(StatusCode.NotFound, "That account no longer exists."));
        if (user.Disabled)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "This account is disabled."));
        if (code.Purpose == CodePurpose.Invitation)
            user.EmailConfirmed = true;

        string ticket = codes.IssueToken(user.Id, CodePurpose.RegisterPasskey, OneTimeCodes.TicketLifetime);
        audit.Record(AuditActions.CodeRedeemed, user.Id, detail: code.Purpose.ToString(), remoteAddress: Address(context));
        await db.SaveChangesAsync(cancellationToken);
        return new TicketReply { Ticket = ticket };
    }

    [EnableRateLimiting(RateLimits.PublicWrites)]
    public async ValueTask<Done> RequestRecoveryAsync(RequestRecoveryRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        AccountUser? user = MailAddress.TryCreate(request.Email.Trim(), out MailAddress? address)
            ? await users.FindByEmailAsync(address.Address)
            : null;

        if (user is { EmailConfirmed: true, Disabled: false, Email: not null })
        {
            string link = Link(codes.IssueToken(user.Id, CodePurpose.Recovery, OneTimeCodes.RecoveryLifetime));
            audit.Record(AuditActions.RecoveryRequested, user.Id, remoteAddress: Address(context));
            await db.SaveChangesAsync(cancellationToken);
            await email.SendAsync(AccountEmails.Recover(user.Email, user.DisplayName, link), cancellationToken);
        }

        return new Done();
    }

    public async ValueTask<Profile> GetProfileAsync(Nothing request, CallContext context = default)
    {
        AccountUser user = await CurrentUserAsync(context);
        return new Profile
        {
            UserId = user.Id.ToString(),
            DisplayName = user.DisplayName,
            Email = user.Email ?? "",
            EmailConfirmed = user.EmailConfirmed,
        };
    }

    public async ValueTask<Done> UpdateNameAsync(UpdateNameRequest request, CallContext context = default)
    {
        AccountUser user = await CurrentUserAsync(context);
        user.DisplayName = ValidName(request.DisplayName);
        audit.Record(AuditActions.NameChanged, user.Id);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> ConfirmEmailAsync(ConfirmEmailRequest request, CallContext context = default)
    {
        AccountUser user = await CurrentUserAsync(context);
        if (!await codes.ConsumeDigitsAsync(user.Id, CodePurpose.ConfirmEmail, request.Code, context.CancellationToken))
        {
            await db.SaveChangesAsync(context.CancellationToken);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "That code isn't right, or has expired. Send a new one."));
        }

        user.EmailConfirmed = true;
        audit.Record(AuditActions.EmailConfirmed, user.Id);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> ResendConfirmationAsync(Nothing request, CallContext context = default)
    {
        AccountUser user = await CurrentUserAsync(context);
        if (user.EmailConfirmed || user.Email is null)
            return new Done();

        string code = await codes.IssueDigitsAsync(user.Id, CodePurpose.ConfirmEmail, OneTimeCodes.ConfirmationLifetime, context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);
        await email.SendAsync(AccountEmails.Confirm(user.Email, user.DisplayName, code), context.CancellationToken);
        return new Done();
    }

    public async ValueTask<PasskeyList> ListPasskeysAsync(Nothing request, CallContext context = default)
    {
        AccountUser user = await CurrentUserAsync(context);
        IList<UserPasskeyInfo> passkeys = await users.GetPasskeysAsync(user);
        return new PasskeyList
        {
            Passkeys = passkeys.OrderBy(passkey => passkey.CreatedAt).Select(passkey => new PasskeySummary
            {
                Id = WebEncoders.Base64UrlEncode(passkey.CredentialId),
                Name = passkey.Name ?? "Passkey",
                CreatedAt = passkey.CreatedAt.UtcDateTime,
                BackedUp = passkey.IsBackedUp,
            }).ToList(),
        };
    }

    public async ValueTask<Done> RemovePasskeyAsync(RemovePasskeyRequest request, CallContext context = default)
    {
        AccountUser user = await CurrentUserAsync(context);
        IList<UserPasskeyInfo> passkeys = await users.GetPasskeysAsync(user);
        if (passkeys.Count <= 1)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Add another passkey before removing your last one."));

        byte[] credentialId = WebEncoders.Base64UrlDecode(request.Id);
        if (!passkeys.Any(passkey => passkey.CredentialId.AsSpan().SequenceEqual(credentialId)))
            throw new RpcException(new Status(StatusCode.NotFound, "That passkey isn't on your account."));

        await users.RemovePasskeyAsync(user, credentialId);
        audit.Record(AuditActions.PasskeyRemoved, user.Id);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<SessionList> ListSessionsAsync(Nothing request, CallContext context = default)
    {
        (Guid userId, Guid? sessionId) = Caller(context);
        List<AccountSession> sessions = await db.Sessions.AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.LastSeenAt)
            .ToListAsync(context.CancellationToken);

        return new SessionList
        {
            Sessions = sessions.Select(session => new SessionSummary
            {
                Id = session.Id.ToString(),
                Client = session.Client.ToString(),
                DeviceName = session.DeviceName,
                CreatedAt = session.CreatedAt,
                LastSeenAt = session.LastSeenAt,
                Current = session.Id == sessionId,
            }).ToList(),
        };
    }

    public async ValueTask<Done> EndSessionAsync(EndSessionRequest request, CallContext context = default)
    {
        (Guid userId, _) = Caller(context);
        if (!Guid.TryParse(request.Id, out Guid id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "That isn't a session."));

        await db.Sessions.Where(session => session.Id == id && session.UserId == userId).ExecuteDeleteAsync(context.CancellationToken);
        audit.Record(AuditActions.SessionEnded, userId);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SignOutEverywhereAsync(Nothing request, CallContext context = default)
    {
        (Guid userId, _) = Caller(context);
        await db.Sessions.Where(session => session.UserId == userId).ExecuteDeleteAsync(context.CancellationToken);
        audit.Record(AuditActions.SignedOutEverywhere, userId);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> InviteAsync(InviteRequest request, CallContext context = default)
    {
        string name = ValidName(request.DisplayName);
        string address = ValidEmail(request.Email);
        CancellationToken cancellationToken = context.CancellationToken;

        AccountUser user = await FindAbandonedOrCreateAsync(name, address, cancellationToken);
        string link = Link(codes.IssueToken(user.Id, CodePurpose.Invitation, OneTimeCodes.InvitationLifetime));
        audit.Record(AuditActions.Invited, user.Id, Caller(context).UserId);
        await db.SaveChangesAsync(cancellationToken);
        await email.SendAsync(AccountEmails.Invite(address, name, link), cancellationToken);
        return new Done();
    }

    public async ValueTask<CodeReply> IssueRecoveryCodeAsync(IssueRecoveryCodeRequest request, CallContext context = default)
    {
        AccountUser user = (Guid.TryParse(request.UserId, out Guid id) ? await users.FindByIdAsync(id.ToString()) : null)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such account."));

        string code = codes.IssueToken(user.Id, CodePurpose.Recovery, OneTimeCodes.RecoveryLifetime);
        audit.Record(AuditActions.RecoveryIssued, user.Id, Caller(context).UserId);
        await db.SaveChangesAsync(context.CancellationToken);
        return new CodeReply { Code = Link(code), ExpiresAt = time.GetUtcNow().UtcDateTime + OneTimeCodes.RecoveryLifetime };
    }

    private async Task<AccountUser> FindAbandonedOrCreateAsync(string name, string address, CancellationToken cancellationToken)
    {
        AccountUser? existing = await users.FindByEmailAsync(address);
        if (existing is not null)
        {
            if ((await users.GetPasskeysAsync(existing)).Count > 0)
                throw new RpcException(new Status(StatusCode.AlreadyExists, "An account already uses this email. Sign in, or recover it."));
            existing.DisplayName = name;
            return existing;
        }

        Guid id = Guid.CreateVersion7();
        AccountUser user = new()
        {
            Id = id,
            UserName = id.ToString(),
            Email = address,
            DisplayName = name,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        IdentityResult created = await users.CreateAsync(user);
        if (!created.Succeeded)
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(" ", created.Errors.Select(error => error.Description))));
        return user;
    }

    private async Task<AccountUser> CurrentUserAsync(CallContext context) =>
        await users.FindByIdAsync(Caller(context).UserId.ToString())
        ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    private static (Guid UserId, Guid? SessionId) Caller(CallContext context)
    {
        ClaimsPrincipal user = context.ServerCallContext!.GetHttpContext().User;
        return Guid.TryParse(user.FindFirstValue(AccountClaims.UserId), out Guid userId)
            ? (userId, Guid.TryParse(user.FindFirstValue(AccountClaims.SessionId), out Guid sessionId) ? sessionId : null)
            : throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));
    }

    private string Link(string code) => $"{options.Value.SiteUrl.TrimEnd('/')}/welcome?code={code}";

    private static string? Address(CallContext context) =>
        context.ServerCallContext is { } call ? RateLimits.ClientAddress(call.GetHttpContext()) : null;

    private static string ValidName(string name)
    {
        string trimmed = name.Trim();
        return trimmed.Length is > 0 and <= MaximumNameLength
            ? trimmed
            : throw new RpcException(new Status(StatusCode.InvalidArgument, "Enter your name (up to 80 characters)."));
    }

    private static string ValidEmail(string address) =>
        MailAddress.TryCreate(address.Trim(), out MailAddress? parsed) && parsed.Address.Length <= 254
            ? parsed.Address
            : throw new RpcException(new Status(StatusCode.InvalidArgument, "That doesn't look like an email address."));
}
