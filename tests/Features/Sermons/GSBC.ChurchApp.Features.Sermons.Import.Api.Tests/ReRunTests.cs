using System.Net;
using GSBC.ChurchApp.Features.Sermons.Api;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Import.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Import.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Tests;

[Integration("A re-run skips what the import's own table says is already brought across, and the advisory lock and run state carry it through")]
public sealed class ReRunTests : IAsyncLifetime
{
    private const string Harbour = "/media/2026-11-01-safe-harbour-part-4-harbour-of-hope";
    private const string Soil = "/media/2026-10-29-the-good-soil";
    private const string Storm = "/media/2019-05-25-bro-kit-ward-hope-in-a-storm";

    private readonly FakeWebsite _website = new();
    private readonly MemoryBlobs _blobs = new();
    private readonly KeptMedia _media = new();
    private readonly TestClock _clock = new(ChurchTime.ToUtc(new DateTime(2026, 11, 9, 20, 0, 0)));
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
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton<IBlobStore>(_blobs);
        services.AddSingleton<IMediaReferences>(_media);
        services.AddSingleton<IMediaSystemFolders>(_media);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSyncData();
        services.AddSermonsData();
        services.AddSermonsImportData();
        services.AddScoped<Speakers>();
        services.AddScoped<ServiceNames>();
        services.AddScoped<SermonPublisher>();
        services.AddScoped<PodcastFeedWriter>();
        services.AddOptions<PodcastOptions>();
        services.AddOptions<ImportOptions>().Configure(options => (options.WebsiteUrl, options.PageDelay) = (FakeWebsite.BaseUrl, TimeSpan.Zero));
        services.AddSingleton<PageGate>();
        services.AddHttpClient<ArchiveClient>().ConfigurePrimaryHttpMessageHandler(() => _website);
        services.AddScoped<SermonImporter>();
        services.AddSingleton<ImportRunner>();
        services.AddScoped<SermonImportService>();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<SermonsDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<SermonsImportDbContext>().Database.MigrateAsync(Cancel);
        ServeTheSite();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task ARunBringsEverySermonAcrossOnceAndARerunSkipsThemAndRetriesOnlyWhatFailed()
    {
        _website.Fail($"{FakeWebsite.BaseUrl}/podcasts{Soil}", HttpStatusCode.InternalServerError);

        ImportProgress first = await RunAsync();

        Assert.Equal((3, 3, 2, 0, 1), (first.WebsiteCount, first.Found, first.Copied, first.Skipped, first.Failed));
        Assert.Equal(Soil, Assert.Single(first.Failures).WebsitePath);
        List<SermonRecord> sermons = await SermonsAsync();
        Assert.Equal(["Hope In A Storm", "Safe Harbour: Part 4 - Harbour Of Hope"], sermons.Select(sermon => sermon.Title).Order());
        Assert.All(sermons, sermon => Assert.NotNull(sermon.AnnouncedAt));
        SermonRecord harbour = sermons.Single(sermon => sermon.Title.Contains("Harbour", StringComparison.Ordinal));
        Assert.Equal(("Psalm 107:23-30", "Sunday Morning", "AbCdEfGhIj0"), (harbour.Passage, harbour.Service, harbour.YouTubeId));
        SpeakerRecord kit = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Speakers.SingleAsync(speaker => speaker.Name == "Sam Sample", Cancel));
        Assert.Equal("Bro", kit.Honorific);

        _website.ServeText($"{FakeWebsite.BaseUrl}/podcasts{Soil}", SampleFiles.Text("sermon-storm.html")
            .Replace("Bro Sam Sample - Hope In A Storm", "The Good Soil", StringComparison.Ordinal).Replace("May 25, 2019", "October 29, 2026", StringComparison.Ordinal)
            .Replace("2019/05/storm.mp3", "2026/10/soil.mp3", StringComparison.Ordinal));
        _website.Requested.Clear();
        ImportProgress second = await RunAsync();

        Assert.Equal((3, 1, 2, 0), (second.Found, second.Copied, second.Skipped, second.Failed));
        Assert.Equal(3, (await SermonsAsync()).Count);
        Assert.Equal(["https://files.example/2026/10/soil.mp3"], _website.Requested.Where(url => url.EndsWith(".mp3", StringComparison.Ordinal)));

        _website.Requested.Clear();
        ImportProgress third = await RunAsync();

        Assert.Equal((0, 3), (third.Copied, third.Skipped));
        Assert.Equal(3, (await SermonsAsync()).Count);
        Assert.DoesNotContain(_website.Requested, url => url.Contains("/podcasts/media/", StringComparison.Ordinal) || url.EndsWith(".mp3", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheSameRecordingUnderTwoAddressesIsOneSermon()
    {
        _website.ServeBytes("https://files.example/2019/05/storm.mp3", SampleFiles.Mp3(1));

        ImportProgress done = await RunAsync();

        Assert.Equal((2, 1), (done.Copied, done.Skipped));
        Assert.Equal(2, (await SermonsAsync()).Count);
        Assert.Equal(2, _blobs.Objects.Keys.Count(key => key.Item2.EndsWith(".mp3", StringComparison.Ordinal)));
    }

    private void ServeTheSite()
    {
        _website.ServeText($"{FakeWebsite.BaseUrl}/podcasts/media?page=1", SampleFiles.Text("listing-1.html"));
        _website.ServeText($"{FakeWebsite.BaseUrl}/podcasts/media?page=2", SampleFiles.Text("listing-2.html"));
        _website.ServeText($"{FakeWebsite.BaseUrl}/podcasts{Harbour}", SampleFiles.Text("sermon-harbour.html"));
        _website.ServeText($"{FakeWebsite.BaseUrl}/podcasts{Soil}", SampleFiles.Text("sermon-storm.html")
            .Replace("Bro Sam Sample - Hope In A Storm", "The Good Soil", StringComparison.Ordinal).Replace("2019/05/storm.mp3", "2026/10/soil.mp3", StringComparison.Ordinal));
        _website.ServeText($"{FakeWebsite.BaseUrl}/podcasts{Storm}", SampleFiles.Text("sermon-storm.html"));
        _website.ServeText($"{FakeWebsite.BaseUrl}/series/safe-harbour", SampleFiles.Text("series-safe-harbour.html"));
        _website.ServeBytes("https://files.example/2026/11/01/harbour-4.mp3", SampleFiles.Mp3(1));
        _website.ServeBytes("https://files.example/2026/10/soil.mp3", SampleFiles.Mp3(2));
        _website.ServeBytes("https://files.example/2019/05/storm.mp3", SampleFiles.Mp3(3));
        foreach (string picture in new[] { "2026/11/01/art/harbour.png", "2026/11/01/art/harbour-wide.png", "2026/10/series/safe-harbour.png", "2019/art/storm.jpg", "people/ellery.jpg" })
            _website.ServeBytes($"https://files.example/{picture}", SampleFiles.Picture(64, 36));
    }

    private async Task<ImportProgress> RunAsync()
    {
        await WithAsync(provider => provider.GetRequiredService<SermonImportService>().StartAsync(new ImportRequest()).AsTask());
        await _services.GetRequiredService<ImportRunner>().RunAsync(Cancel);
        return await WithAsync(provider => provider.GetRequiredService<SermonImportService>().ProgressAsync(new ImportRequest()).AsTask());
    }

    private Task<List<SermonRecord>> SermonsAsync() =>
        WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking().ToListAsync(Cancel));

    private async Task<T> WithAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }
}
