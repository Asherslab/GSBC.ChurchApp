using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal sealed class MyAccountFeed(AccountsDbContext db, UserManager<AccountUser> users, DemoAccounts demo, TimeProvider time)
{
    public async Task PublishAsync(IReadOnlyCollection<Guid>? only, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<AccountUser> people = await db.Users.AsNoTracking().Where(user => only == null || only.Contains(user.Id)).ToListAsync(cancellationToken);
        List<Guid> ids = people.Select(user => user.Id).ToList();
        ILookup<Guid, AccountSession> sessions = (await db.Sessions.AsNoTracking()
                .Where(session => (only == null || only.Contains(session.UserId)) && session.ExpiresAt > now).ToListAsync(cancellationToken))
            .ToLookup(session => session.UserId);
        List<string> recordIds = [.. (only ?? []).Select(id => id.ToString())];
        Dictionary<string, FeedChangeRecord> published = (await db.FeedChanges.AsNoTracking()
                .Where(change => change.Feed == AccountsFeeds.Mine && (only == null || recordIds.Contains(change.RecordId)))
                .GroupBy(change => change.RecordId)
                .Select(versions => versions.OrderByDescending(change => change.Cursor).First())
                .ToListAsync(cancellationToken))
            .ToDictionary(change => change.RecordId);

        long version = now.Ticks;
        foreach (AccountUser user in people)
        {
            string id = user.Id.ToString();
            if (user.DeletedAt is not null)
            {
                if (published.GetValueOrDefault(id) is { Deleted: false })
                    db.FeedChanges.Add(new FeedChangeRecord { Feed = AccountsFeeds.Mine, RecordId = id, ScopeId = id, Version = version, Deleted = true });
                continue;
            }
            byte[] payload = Encode(await AccountOfAsync(user, sessions[user.Id], cancellationToken));
            if (published.GetValueOrDefault(id) is { Deleted: false } before && before.Payload.AsSpan().SequenceEqual(payload))
                continue;
            db.FeedChanges.Add(new FeedChangeRecord { Feed = AccountsFeeds.Mine, RecordId = id, ScopeId = id, Version = version, Payload = payload });
        }
        foreach (string gone in published.Keys.Except(ids.Select(id => id.ToString())).Where(key => !published[key].Deleted))
            db.FeedChanges.Add(new FeedChangeRecord { Feed = AccountsFeeds.Mine, RecordId = gone, ScopeId = gone, Version = version, Deleted = true });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MyAccount> AccountOfAsync(AccountUser user, IEnumerable<AccountSession> sessions, CancellationToken cancellationToken) => new()
    {
        Profile = new Profile
        {
            UserId = user.Id.ToString(), DisplayName = user.DisplayName, Email = user.Email ?? "", EmailConfirmed = user.EmailConfirmed,
            DemoAccount = await demo.IsDemoAsync(user.Id, cancellationToken),
        },
        Passkeys = new PasskeyList
        {
            Passkeys = (await users.GetPasskeysAsync(user)).OrderBy(passkey => passkey.CreatedAt).Select(passkey => new PasskeySummary
            {
                Id = WebEncoders.Base64UrlEncode(passkey.CredentialId), Name = passkey.Name ?? "Passkey", CreatedAt = passkey.CreatedAt.UtcDateTime,
                BackedUp = passkey.IsBackedUp,
            }).ToList(),
        },
        Sessions = new SessionList
        {
            Sessions = sessions.OrderByDescending(session => session.LastSeenAt).Select(session => new SessionSummary
            {
                Id = session.Id.ToString(), Client = session.Client.ToString(), DeviceName = SessionDevices.Describe(session.Client, session.DeviceName),
                CreatedAt = session.CreatedAt, LastSeenAt = DateTime.SpecifyKind(session.LastSeenAt.Date, DateTimeKind.Utc),
            }).ToList(),
        },
    };

    private static byte[] Encode(MyAccount account)
    {
        using MemoryStream stream = new();
        AccountsContractsModel.Instance.Serialize(stream, account);
        return stream.ToArray();
    }
}
