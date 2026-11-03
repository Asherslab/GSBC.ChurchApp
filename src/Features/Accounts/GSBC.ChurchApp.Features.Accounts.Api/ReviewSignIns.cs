using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class ReviewSignIns(AccountsDbContext db, UserManager<AccountUser> users, DemoAccounts demo, AccountsAudit audit, TimeProvider time)
{
    public static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(15);
    public const int WrongGuessesPerAccount = 5;
    public const int WrongGuessesPerAddress = 10;

    private const string NoMatch = "That email and sign-in code don't match. Check them and try again.";
    private const string NotLive = "That sign-in code has expired or was turned off. Ask the church for a new one.";
    private const string Throttled = "Too many wrong sign-in codes. Wait 15 minutes, then try again.";

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async Task<string> SignInAsync(string email, string typedCode, string? device, string? remoteAddress, CancellationToken cancellationToken)
    {
        AccountUser? user = await users.FindByEmailAsync(email.Trim());
        if (await IsThrottledAsync(user?.Id, remoteAddress, cancellationToken))
            throw new RpcException(new Status(StatusCode.ResourceExhausted, Throttled));

        string hash = ReviewCodes.HashOf(typedCode);
        ReviewCode? code = user is null ? null : await db.ReviewCodes.SingleOrDefaultAsync(candidate => candidate.Hash == hash && candidate.UserId == user.Id, cancellationToken);
        if (user is null || code is null)
            throw await RefuseAsync(user?.Id, remoteAddress, "no matching code", NoMatch, cancellationToken);
        if (!code.IsLive(Now))
            throw await RefuseAsync(user.Id, remoteAddress, "code not live", NotLive, cancellationToken);
        if (await demo.WhyCodesCannotSignInAsync(user, cancellationToken) is { } problem)
            throw await RefuseAsync(user.Id, remoteAddress, problem, NoMatch, cancellationToken);

        code.Uses++;
        code.LastUsedAt = Now;
        string secret = AppSessions.Start(db, user.Id, device, Now, code.Id);
        audit.Record(AuditActions.ReviewCodeUsed, user.Id, detail: code.Label, remoteAddress: remoteAddress);
        await db.SaveChangesAsync(cancellationToken);
        return secret;
    }

    public async Task<ReviewAccess> OverviewAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> demoIds = await demo.AllAsync(cancellationToken);
        List<AccountUser> accounts = await db.Users.AsNoTracking().Where(user => demoIds.Contains(user.Id)).OrderBy(user => user.DisplayName).ToListAsync(cancellationToken);
        List<ReviewCode> codes = await db.ReviewCodes.AsNoTracking().OrderByDescending(code => code.CreatedAt).ToListAsync(cancellationToken);
        List<Guid> creatorIds = codes.Select(code => code.CreatedBy).Distinct().ToList();
        Dictionary<Guid, string> creators = await db.Users.AsNoTracking().Where(user => creatorIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.DisplayName, cancellationToken);

        ReviewAccess overview = new()
        {
            Codes = codes.Select(code => new ReviewCodeInfo
            {
                Id = code.Id.ToString(),
                UserId = code.UserId.ToString(),
                Label = code.Label,
                CreatedBy = creators.GetValueOrDefault(code.CreatedBy, "Someone"),
                CreatedAt = code.CreatedAt,
                ExpiresAt = code.ExpiresAt,
                TurnedOffAt = code.TurnedOffAt,
                Uses = code.Uses,
                LastUsedAt = code.LastUsedAt,
            }).ToList(),
        };
        foreach (AccountUser account in accounts)
        {
            overview.DemoAccounts.Add(new DemoAccountInfo
            {
                UserId = account.Id.ToString(),
                DisplayName = account.DisplayName,
                Email = account.Email ?? "",
                Problem = await demo.WhyCodesCannotSignInAsync(account, cancellationToken),
            });
        }
        return overview;
    }

    public async Task<CodeReply> CreateCodeAsync(Guid actorId, string userId, string label, int days, CancellationToken cancellationToken)
    {
        AccountUser user = (Guid.TryParse(userId, out Guid id) ? await users.FindByIdAsync(id.ToString()) : null)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such account."));
        if (await demo.WhyCodesCannotSignInAsync(user, cancellationToken) is { } problem)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, problem));

        TimeSpan lifetime = days == 0 ? ReviewCodes.DefaultLifetime : TimeSpan.FromDays(days);
        if (lifetime <= TimeSpan.Zero || lifetime > ReviewCodes.LongestLifetime)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"A code can last from 1 to {ReviewCodes.LongestLifetime.Days} days."));
        string trimmed = label.Trim();
        if (trimmed.Length > ReviewCodes.LabelLength)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Keep the label under {ReviewCodes.LabelLength} characters."));

        string code = ReviewCodes.NewCode();
        DateTime expiresAt = Now + lifetime;
        db.ReviewCodes.Add(new ReviewCode
        {
            Id = Guid.CreateVersion7(),
            Hash = ReviewCodes.HashOf(code),
            UserId = user.Id,
            Label = trimmed.Length == 0 ? "App review" : trimmed,
            CreatedBy = actorId,
            CreatedAt = Now,
            ExpiresAt = expiresAt,
        });
        audit.Record(AuditActions.ReviewCodeCreated, user.Id, actorId, detail: trimmed);
        await db.SaveChangesAsync(cancellationToken);
        return new CodeReply { Code = code, ExpiresAt = expiresAt };
    }

    public async Task TurnOffAsync(Guid actorId, string codeId, CancellationToken cancellationToken)
    {
        ReviewCode code = (Guid.TryParse(codeId, out Guid id) ? await db.ReviewCodes.FindAsync([id], cancellationToken) : null)
            ?? throw new RpcException(new Status(StatusCode.NotFound, "No such sign-in code."));
        if (code.TurnedOffAt is not null)
            return;

        code.TurnedOffAt = Now;
        await db.Sessions.Where(session => session.ReviewCodeId == code.Id).ExecuteDeleteAsync(cancellationToken);
        audit.Record(AuditActions.ReviewCodeTurnedOff, code.UserId, actorId, detail: code.Label);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsThrottledAsync(Guid? userId, string? remoteAddress, CancellationToken cancellationToken)
    {
        DateTime since = Now - ThrottleWindow;
        IQueryable<AuditEntry> refusals = db.Audit.Where(entry => entry.Action == AuditActions.ReviewCodeRefused && entry.At > since);
        return (userId is not null && await refusals.CountAsync(entry => entry.UserId == userId, cancellationToken) >= WrongGuessesPerAccount)
            || (remoteAddress is not null && await refusals.CountAsync(entry => entry.RemoteAddress == remoteAddress, cancellationToken) >= WrongGuessesPerAddress);
    }

    private async Task<RpcException> RefuseAsync(Guid? userId, string? remoteAddress, string reason, string message, CancellationToken cancellationToken)
    {
        audit.Record(AuditActions.ReviewCodeRefused, userId, detail: reason, remoteAddress: remoteAddress);
        await db.SaveChangesAsync(cancellationToken);
        return new RpcException(new Status(StatusCode.Unauthenticated, message));
    }
}
