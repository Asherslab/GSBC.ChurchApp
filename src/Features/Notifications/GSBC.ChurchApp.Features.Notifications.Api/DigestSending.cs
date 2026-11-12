using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class DigestSending(
    NotificationsDbContext db, NotificationRaiser raiser, NotificationKindCatalog kinds, PermissionCatalog permissions, IPermissionHolders holders,
    IPeopleIndex people, TimeProvider time)
{
    public async Task<int> SendDueAsync(CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<Guid> due = await db.Database.SqlQuery<Guid>($"""
            UPDATE notifications.digests SET due_at = NULL
            WHERE due_at <= {now}
            RETURNING id AS "Value"
            """).ToListAsync(cancellationToken);
        foreach (Guid id in due)
            await SendAsync(id, now, cancellationToken);
        return due.Count;
    }

    private async Task SendAsync(Guid id, DateTime now, CancellationToken cancellationToken)
    {
        DigestRecord digest = await db.Digests.SingleAsync(found => found.Id == id, cancellationToken);
        List<DigestItemRecord> items = await db.DigestItems.AsNoTracking()
            .Where(item => item.DigestId == id && item.Id > digest.SentThrough)
            .ToListAsync(cancellationToken);
        if (items.Count == 0 || !kinds.ByKey.TryGetValue(digest.Kind, out NotificationKind? kind) || kind.Digest is not { } wording
            || DigestAudiences.Read(digest, permissions) is not { } audience)
            return;

        Notification told = new(kind, audience, "", wording.Body, digest.Link, $"digest/{kind.Key}/{digest.GroupKey}", ScopeId: digest.ScopeId,
            Action: digest.Action) { Actor = null };
        Recipients everyone = await NotificationRules.RecipientsAsync(told, holders, people, cancellationToken);
        foreach ((int count, Recipients recipients) in Counted(everyone, items))
            await raiser.RaiseToAsync(told with { Title = wording.Title(count) }, recipients, cancellationToken);

        digest.SentThrough = items.Max(item => item.Id);
        digest.LastSentAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<(int Count, Recipients Recipients)> Counted(Recipients everyone, IReadOnlyCollection<DigestItemRecord> items)
    {
        Dictionary<int, List<Guid>> byCount = everyone.People
            .GroupBy(person => items.Count(item => item.ActorId != person))
            .ToDictionary(group => group.Key, group => group.ToList());
        bool phones = everyone.EveryPhone || everyone.Phones.Count > 0;
        if (phones)
            byCount.TryAdd(items.Count, []);

        foreach ((int count, List<Guid> counted) in byCount.Where(group => group.Key > 0).OrderByDescending(group => group.Key))
            yield return (count, count == items.Count && phones
                ? everyone with { People = counted }
                : new Recipients(counted, [], EveryPhone: false, Public: false));
    }
}

internal sealed class DigestSender(IServiceScopeFactory scopes, TimeProvider time, ILogger<DigestSender> logger) : BackgroundService
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Every, time);
        do
        {
            try
            {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<DigestSending>().SendDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Sending digests failed; trying again in a minute");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
