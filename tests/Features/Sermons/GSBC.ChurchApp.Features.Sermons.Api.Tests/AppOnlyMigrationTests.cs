using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("The app-only migration deletes the website's sermons and the series and speakers only they used, in SQL, with a feed delete for each")]
public sealed class AppOnlyMigrationTests : IAsyncLifetime
{
    private static readonly Guid FromWebsite = Guid.CreateVersion7(), Uploaded = Guid.CreateVersion7();
    private static readonly Guid WebsiteSeries = Guid.CreateVersion7(), SharedSeries = Guid.CreateVersion7();
    private static readonly Guid WebsiteSpeaker = Guid.CreateVersion7(), UploadSpeaker = Guid.CreateVersion7(), CorrectedTo = Guid.CreateVersion7();

    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
            .Build());
        services.AddSyncData();
        services.AddSermonsData();
        _services = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task TheWebsitesSermonsGoAndUploadedOnesKeepWhatStaffChose()
    {
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
            SermonsDbContext db = scope.ServiceProvider.GetRequiredService<SermonsDbContext>();
            await db.GetService<IMigrator>().MigrateAsync("20261109101955_SpeakerCorrected", cancellationToken: Cancel);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO sermons.series (id, slug, name, description, created_at, published_hash) VALUES
                    ({WebsiteSeries}, 'safe-harbour', 'Safe Harbour', '', now(), 'published'),
                    ({SharedSeries}, 'fruit', 'Fruit Of The Spirit', '', now(), 'published');
                INSERT INTO sermons.speakers (id, name, created_at, published_hash) VALUES
                    ({WebsiteSpeaker}, 'Jo Ellery', now(), 'published'),
                    ({UploadSpeaker}, 'Ruth Calder', now(), 'published'),
                    ({CorrectedTo}, 'Ps Ruth Calder', now(), 'published');
                INSERT INTO sermons.sermons (id, origin, preached_at, added_at, updated_at, source_title, source_speaker, source_speaker_id, source_series_id,
                        source_service, summary, title, speaker_corrected, speaker_id, series_corrected, series_id, service, you_tube_corrected, hidden,
                        audio_url, audio_type, audio_bytes, duration_seconds, copied_bytes, published_hash) VALUES
                    ({FromWebsite}, 'website', now(), now(), now(), 'Harbour Of Hope', 'Jo Ellery', {WebsiteSpeaker}, {WebsiteSeries},
                        'Sunday Morning', '', NULL, false, NULL, false, NULL, NULL, false, false, 'https://files.example/hope.mp3', 'audio/mpeg', 1, 1, 0, 'published'),
                    ({Uploaded}, 'uploads', now(), now(), now(), 'The Good Soil', 'Ruth Calder', {UploadSpeaker}, NULL,
                        'Sunday Evening', 'About soil', 'The Good Soil, Again', true, {CorrectedTo}, true, {SharedSeries}, NULL, false, false,
                        'https://media.example/soil.mp3', 'audio/mpeg', 1, 1, 0, 'published');
                """, Cancel);
            await db.Database.MigrateAsync(Cancel);
        }

        await using AsyncServiceScope after = _services.CreateAsyncScope();
        SermonsDbContext migrated = after.ServiceProvider.GetRequiredService<SermonsDbContext>();
        SermonRecord kept = Assert.Single(await migrated.Sermons.AsNoTracking().ToListAsync(Cancel));
        Assert.Equal((Uploaded, "The Good Soil, Again", CorrectedTo, SharedSeries, "Sunday Evening"), (kept.Id, kept.Title, kept.SpeakerId, kept.SeriesId, kept.Service));
        Assert.Equal([SharedSeries], await migrated.Series.Select(series => series.Id).ToListAsync(Cancel));
        Assert.Equal(new[] { UploadSpeaker, CorrectedTo }.Order(), (await migrated.Speakers.Select(speaker => speaker.Id).ToListAsync(Cancel)).Order());
        List<(string, string)> deletes = (await migrated.FeedChanges.Where(change => change.Deleted).ToListAsync(Cancel)).Select(change => (change.Feed, change.RecordId)).ToList();
        Assert.Equal(new[] { (SermonsFeeds.Catalogue, FromWebsite.ToString()), (SermonsFeeds.Series, WebsiteSeries.ToString()), (SermonsFeeds.Speakers, WebsiteSpeaker.ToString()) }.Order(),
            deletes.Order());
    }
}
