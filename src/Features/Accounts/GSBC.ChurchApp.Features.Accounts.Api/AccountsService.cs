using System.Net.Mail;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Permissions;
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
    DemoAccounts demo,
    ReviewSignIns reviews,
    Invitations invitations,
    TimeProvider time) : IAccountsService
{
    private const int MaximumNameLength = 80;
    private const string DemoAccountsUseCodes = "This is a demo account for app reviewers. It signs in with a sign-in code, never a passkey.";
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
        if (await demo.IsDemoAsync(user.Id, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, DemoAccountsUseCodes));
        if (code.Purpose == CodePurpose.Invitation)
            user.EmailConfirmed = true;

        string ticket = codes.IssueToken(user.Id, CodePurpose.RegisterPasskey, OneTimeCodes.TicketLifetime);
        audit.Record(AuditActions.CodeRedeemed, user.Id, detail: code.Purpose.ToString(), remoteAddress: Address(context));
        await db.SaveChangesAsync(cancellationToken);
        return new TicketReply { Ticket = ticket };
    }

    [EnableRateLimiting(RateLimits.PublicWrites)]
    public async ValueTask<CodeSession> SignInWithCodeAsync(SignInWithCodeRequest request, CallContext context = default) =>
        new() { Session = await reviews.SignInAsync(request.Email, request.Code, request.Device, Address(context), context.CancellationToken) };

    [EnableRateLimiting(RateLimits.PublicWrites)]
    public async ValueTask<Done> RequestRecoveryAsync(RequestRecoveryRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        AccountUser? user = MailAddress.TryCreate(request.Email.Trim(), out MailAddress? address)
            ? await users.FindByEmailAsync(address.Address)
            : null;

        if (user is { EmailConfirmed: true, Disabled: false, Email: not null } 
            && !await demo.HoldsSensitivePermissionAsync(user.Id, cancellationToken) && !await demo.IsDemoAsync(user.Id, cancellationToken))
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
            DemoAccount = await demo.IsDemoAsync(user.Id, context.CancellationToken),
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

    public async ValueTask<ToolConnection> ConnectToolAsync(ConnectToolRequest request, CallContext context = default)
    {
        (Guid userId, _) = Caller(context);
        string name = request.Name.Trim();
        if (name.Length is 0 or > AppSessions.MaximumToolNameLength)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Name the tool, in up to {AppSessions.MaximumToolNameLength} characters, such as Claude on your Mac."));

        string session = AppSessions.StartTool(db, userId, name, time.GetUtcNow().UtcDateTime);
        audit.Record(AuditActions.ToolConnected, userId, detail: name, remoteAddress: Address(context));
        await db.SaveChangesAsync(context.CancellationToken);
        return new ToolConnection { Session = session };
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
        await invitations.SendAsync(user, Caller(context).UserId, AuditActions.Invited, cancellationToken);
        return new Done();
    }

    public async ValueTask<InvitationList> ListInvitationsAsync(Nothing request, CallContext context = default) =>
        await invitations.ListAsync(context.CancellationToken);

    public async ValueTask<Done> ResendInvitationAsync(InvitationRequest request, CallContext context = default)
    {
        await invitations.ResendAsync(request.UserId, Caller(context).UserId, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> WithdrawInvitationAsync(InvitationRequest request, CallContext context = default)
    {
        await invitations.WithdrawAsync(request.UserId, Caller(context).UserId, context.CancellationToken);
        return new Done();
    }

    public async ValueTask<CodeReply> IssueRecoveryCodeAsync(IssueRecoveryCodeRequest request, CallContext context = default)
    {
        AccountUser user = (Guid.TryParse(request.UserId, out Guid id) ? await users.FindByIdAsync(id.ToString()) : null)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such account."));
        if (await demo.IsDemoAsync(user.Id, context.CancellationToken))
            throw new RpcException(new Status(StatusCode.FailedPrecondition, DemoAccountsUseCodes));

        string code = codes.IssueToken(user.Id, CodePurpose.Recovery, OneTimeCodes.RecoveryLifetime);
        audit.Record(AuditActions.RecoveryIssued, user.Id, Caller(context).UserId);
        await db.SaveChangesAsync(context.CancellationToken);
        return new CodeReply { Code = Link(code), ExpiresAt = time.GetUtcNow().UtcDateTime + OneTimeCodes.RecoveryLifetime };
    }

    public async ValueTask<PeopleList> SearchPeopleAsync(SearchPeopleRequest request, CallContext context = default)
    {
        const int MaximumResults = 50;
        string query = request.Query.Trim().ToUpperInvariant();
        List<AccountUser> found = await db.Users.AsNoTracking()
            .Where(user => query.Length == 0 || user.NormalizedEmail!.Contains(query) || user.DisplayName.ToUpper().Contains(query))
            .OrderBy(user => user.DisplayName)
            .Take(MaximumResults)
            .ToListAsync(context.CancellationToken);
        return await PeopleAsync(found, context.CancellationToken);
    }

    public async ValueTask<PeopleList> GetPeopleAsync(GetPeopleRequest request, CallContext context = default)
    {
        List<Guid> ids = request.Ids.Select(id => Guid.TryParse(id, out Guid parsed) ? parsed : Guid.Empty).Where(id => id != Guid.Empty).ToList();
        List<AccountUser> found = await db.Users.AsNoTracking().Where(user => ids.Contains(user.Id)).ToListAsync(context.CancellationToken);
        return await PeopleAsync(found, context.CancellationToken);
    }

    public async ValueTask<Done> SetDisabledAsync(SetDisabledRequest request, CallContext context = default)
    {
        Guid actorId = Caller(context).UserId;
        AccountUser user = (Guid.TryParse(request.UserId, out Guid id) ? await users.FindByIdAsync(id.ToString()) : null)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such account."));
        if (user.Id == actorId && request.Disabled)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "You can't disable your own account."));

        user.Disabled = request.Disabled;
        if (request.Disabled)
            await db.Sessions.Where(session => session.UserId == user.Id).ExecuteDeleteAsync(context.CancellationToken);
        audit.Record(request.Disabled ? AuditActions.Disabled : AuditActions.Enabled, user.Id, actorId);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<AuditList> ListAuditAsync(AuditQuery request, CallContext context = default)
    {
        const int MaximumEntries = 200;
        IQueryable<AuditEntry> query = db.Audit.AsNoTracking();
        if (Guid.TryParse(request.UserId, out Guid userId))
            query = query.Where(entry => entry.UserId == userId || entry.ActorId == userId);

        return new AuditList
        {
            Entries = await query.OrderByDescending(entry => entry.Id).Take(MaximumEntries).Select(entry => new AuditInfo
            {
                At = entry.At,
                Action = entry.Action,
                UserId = entry.UserId.ToString(),
                ActorId = entry.ActorId.ToString(),
                Detail = entry.Detail,
            }).ToListAsync(context.CancellationToken),
        };
    }

    public async ValueTask<ReviewAccess> GetReviewAccessAsync(Nothing request, CallContext context = default) =>
        await reviews.OverviewAsync(context.CancellationToken);

    public async ValueTask<Done> CreateDemoAccountAsync(CreateDemoAccountRequest request, CallContext context = default)
    {
        string name = ValidName(request.DisplayName);
        string address = ValidEmail(request.Email);
        CancellationToken cancellationToken = context.CancellationToken;
        if (await users.FindByEmailAsync(address) is not null)
            throw new RpcException(new Status(StatusCode.AlreadyExists, "An account already uses this email. Give the demo account an address of its own."));

        AccountUser user = await FindAbandonedOrCreateAsync(name, address, cancellationToken);
        audit.Record(AuditActions.DemoAccountCreated, user.Id, Caller(context).UserId);
        await db.SaveChangesAsync(cancellationToken);
        await demo.MakeAsync(user.Id, cancellationToken);
        return new Done();
    }

    public async ValueTask<CodeReply> CreateReviewCodeAsync(CreateReviewCodeRequest request, CallContext context = default) =>
        await reviews.CreateCodeAsync(Caller(context).UserId, request.UserId, request.Label, request.Days, context.CancellationToken);

    public async ValueTask<Done> TurnOffReviewCodeAsync(TurnOffReviewCodeRequest request, CallContext context = default)
    {
        await reviews.TurnOffAsync(Caller(context).UserId, request.Id, context.CancellationToken);
        return new Done();
    }

    private async Task<PeopleList> PeopleAsync(List<AccountUser> found, CancellationToken cancellationToken)
    {
        List<Guid> ids = found.Select(user => user.Id).ToList();
        HashSet<Guid> withPasskeys = (await db.Set<IdentityUserPasskey<Guid>>().AsNoTracking()
            .Where(passkey => ids.Contains(passkey.UserId)).Select(passkey => passkey.UserId).Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        return new PeopleList
        {
            People = found.Select(user => new PersonInfo
            {
                Id = user.Id.ToString(),
                DisplayName = user.DisplayName,
                Email = user.Email ?? "",
                EmailConfirmed = user.EmailConfirmed,
                Disabled = user.Disabled,
                HasPasskey = withPasskeys.Contains(user.Id),
                CreatedAt = user.CreatedAt,
            }).ToList(),
        };
    }

    private async Task<AccountUser> FindAbandonedOrCreateAsync(string name, string address, CancellationToken cancellationToken)
    {
        AccountUser? existing = await users.FindByEmailAsync(address);
        if (existing is not null)
        {
            if ((await users.GetPasskeysAsync(existing)).Count > 0 || await demo.IsDemoAsync(existing.Id, cancellationToken))
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
