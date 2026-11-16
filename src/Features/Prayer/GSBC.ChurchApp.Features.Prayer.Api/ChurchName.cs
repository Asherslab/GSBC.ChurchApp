using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class ChurchName(PrayerDbContext db, PrayerPublisher publisher, TimeProvider time)
{
    private const int Row = 1;
    private const string RecordId = "church";

    public async Task<string> CurrentAsync(CancellationToken cancellationToken) =>
        (await db.Church.AsNoTracking().FirstOrDefaultAsync(church => church.Id == Row, cancellationToken))?.Name ?? PrayerRules.ChurchNameToStart;

    public async Task RenameAsync(string name, CancellationToken cancellationToken)
    {
        string trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed.Length > PrayerRules.LongestChurchName)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Enter a church name of up to {PrayerRules.LongestChurchName} characters."));
        ChurchRecord? church = await db.Church.FirstOrDefaultAsync(row => row.Id == Row, cancellationToken);
        if (church?.Name == trimmed)
            return;
        if (church is null)
            db.Church.Add(new ChurchRecord { Id = Row, Name = trimmed });
        else
            church.Name = trimmed;

        DateTime now = time.GetUtcNow().UtcDateTime;
        foreach (RequestRecord request in await db.Requests.Where(request => request.FromChurch).ToListAsync(cancellationToken))
        {
            request.Name = trimmed;
            await publisher.ChangedAsync(request, now, cancellationToken);
        }
        Publish(trimmed, now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FillFeedAsync(CancellationToken cancellationToken)
    {
        if (await db.FeedChanges.AnyAsync(change => change.Feed == PrayerFeeds.Church, cancellationToken))
            return;
        Publish(await CurrentAsync(cancellationToken), time.GetUtcNow().UtcDateTime);
        await db.SaveChangesAsync(cancellationToken);
    }

    private void Publish(string name, DateTime now) =>
        db.FeedChanges.Add(new FeedChangeRecord
            { Feed = PrayerFeeds.Church, RecordId = RecordId, Version = now.Ticks, Payload = PrayerPayloads.Encode(new PrayerChurch { Name = name }) });
}
