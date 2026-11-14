using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachSettingsStore(OutreachDbContext db, OutreachMinistries ministries, OutreachPublisher publisher)
{
    public const int OnlyId = 1;
    private OutreachSettings? _settings;

    public async Task<OutreachSettings> GetAsync(CancellationToken cancellationToken) =>
        _settings ??= await db.Settings.AsNoTracking().FirstOrDefaultAsync(settings => settings.Id == OnlyId, cancellationToken) is { } stored
            ? OutreachPayloads.Decode<OutreachSettings>(stored.Payload)
            : OutreachDefaults.Settings();

    public async Task SaveAsync(OutreachSettings settings, Guid? by, DateTime now, CancellationToken cancellationToken)
    {
        settings.MinistryIds = [];
        settings.ChangedAt = now;
        byte[] payload = OutreachPayloads.Encode(settings);
        if (await db.Settings.FirstOrDefaultAsync(stored => stored.Id == OnlyId, cancellationToken) is { } existing)
            (existing.Payload, existing.ChangedAt, existing.ChangedBy) = (payload, now, by);
        else
            db.Settings.Add(new SettingsRecord { Id = OnlyId, Payload = payload, ChangedAt = now, ChangedBy = by });
        _settings = settings;
        await PublishAsync(now, cancellationToken);
    }

    public async Task PublishAsync(DateTime now, CancellationToken cancellationToken)
    {
        OutreachSettings settings = OutreachPayloads.Decode<OutreachSettings>(OutreachPayloads.Encode(await GetAsync(cancellationToken)));
        settings.MinistryIds = (await ministries.RunningAsync(cancellationToken)).Select(ministry => ministry.Id.ToString()).ToList();
        publisher.Settings(settings, now);
    }
}
