using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

public sealed class DiagnosticsInboxTests : IAsyncLifetime
{
    private static readonly Guid Owner = Guid.NewGuid();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly MovableClock _clock = new(new DateTimeOffset(2026, 11, 1, 23, 0, 0, TimeSpan.Zero));
    private readonly RecordingEmail _email = new();
    private ServiceProvider _services = null!;

    private int Today => ChurchTime.Today(_clock).DayNumber;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Database.ConnectionStringName}"] = _postgres.GetConnectionString(),
            ["Accounts:SiteUrl"] = "https://uat-app.baptist.com.au",
        }).Build();

        _services = new ServiceCollection()
            .AddSingleton(configuration)
            .AddLogging()
            .AddSingleton<TimeProvider>(_clock)
            .AddSingleton<IPermissionHolders>(new Holders([Owner]))
            .AddSingleton<IAccountEmails>(new Emails(new Dictionary<Guid, string> { [Owner] = "owner@example.org" }))
            .AddSingleton<IEmailSender>(_email)
            .AddAnalyticsData()
            .AddSingleton<AnalyticsEnvironment>()
            .AddScoped<NewErrorKinds>()
            .AddScoped<DiagnosticsInbox>()
            .AddScoped<AnalyticsRetention>()
            .AddScoped<HealthService>()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>().Database.MigrateAsync(Cancellation);
    }

    [Fact]
    public async Task TheSameErrorFromManyPhonesIsOneKindAResentReportCountsOnceAndTheOwnerHearsOnce()
    {
        await AcceptAsync(Report("1.0", "41", Crash()), requestId: "phone-a-1");
        await AcceptAsync(Report("1.0", "41", Crash()), requestId: "phone-a-1");
        await AcceptAsync(Report("1.0", "42", Crash()), requestId: "phone-b-1");
        await AcceptAsync(Report("1.0", "42", Crash(type: "System.InvalidOperationException")), requestId: "phone-b-2");

        HealthOverview overview = await OverviewAsync();

        ErrorKindSummary nullReference = Assert.Single(overview.Kinds, kind => kind.ExceptionType == "System.NullReferenceException");
        Assert.Equal(2, nullReference.Count);
        Assert.Equal("GSBC.ChurchApp.Features.Sermons.Client.Player.Start", nullReference.Where);
        Assert.Equal(2, overview.Kinds.Count);
        Assert.Equal("uat-app.baptist.com.au", overview.Environment);

        Assert.Equal(2, _email.Sent.Count);
        Assert.All(_email.Sent, sent => Assert.Equal("owner@example.org", sent.To));
        Assert.Contains($"https://uat-app.baptist.com.au/health/{nullReference.Id}", _email.Sent[0].Body);
    }

    [Fact]
    public async Task OnlyTheAppAndTheAdminPanelMayReportAndOnlyAboutRecentDays()
    {
        DiagnosticsReport claimingToBeTheServer = Report("1.0", "41", Crash());
        claimingToBeTheServer.Context!.Origin = ReportOrigin.Server;
        DiagnosticsReport fromLastYear = Report("1.0", "41", Crash(day: Today - 365));
        fromLastYear.FailedCalls.Add(new FailedCallCount { Day = Today - 365, Method = "/gsbc.sync.Sync/Pull", Status = 14, Count = 3 });

        await AcceptAsync(claimingToBeTheServer, requestId: null);
        await AcceptAsync(fromLastYear, requestId: null);

        HealthOverview overview = await OverviewAsync();
        Assert.Empty(overview.Kinds);
        Assert.Empty(overview.FailedCalls);
    }

    [Fact]
    public async Task FailedCallsAddUpPerMethodAndStatusFromTheServerAndThePhones()
    {
        DiagnosticsReport phone = Report("1.0", "41");
        phone.FailedCalls.Add(new FailedCallCount { Day = Today, Method = "/gsbc.sync.Sync/Pull", Status = 13, Count = 3 });
        await AcceptAsync(phone, requestId: null);
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            DiagnosticsInbox inbox = scope.ServiceProvider.GetRequiredService<DiagnosticsInbox>();
            await inbox.RecordServerFailureAsync("/gsbc.sync.Sync/Pull", Grpc.Core.StatusCode.Internal, null, Cancellation);
            await inbox.RecordServerFailureAsync("/gsbc.sync.Sync/Pull", Grpc.Core.StatusCode.Unknown, new InvalidOperationException("Sam's email is sam@example.org"), Cancellation);
        }

        HealthOverview overview = await OverviewAsync();

        Assert.Equal(3, Assert.Single(overview.FailedCalls, call => call.Origin == ReportOrigin.App).Count);
        Assert.Equal(1, Assert.Single(overview.FailedCalls, call => call.Origin == ReportOrigin.Server && call.Status == 13).Count);
        ErrorKindSummary server = Assert.Single(overview.Kinds);
        Assert.Equal(ReportOrigin.Server, server.Origin);
        await using AsyncServiceScope check = _services.CreateAsyncScope();
        string stored = string.Join(' ', await check.ServiceProvider.GetRequiredService<AnalyticsDbContext>().ErrorKinds
            .Select(kind => kind.ExceptionType + string.Join(' ', kind.Frames) + kind.Where).ToListAsync(Cancellation));
        Assert.DoesNotContain("sam@example.org", stored);
    }

    [Fact]
    public async Task ErrorsGoAfterThirtyDaysAndAResentReportIdIsForgottenAfterSeven()
    {
        await AcceptAsync(Report("1.0", "41", Crash()), requestId: "phone-a-1");

        _clock.Advance(TimeSpan.FromDays(8));
        await PruneAsync();
        await AcceptAsync(Report("1.0", "41", Crash()), requestId: "phone-a-1");
        Assert.Equal(2, Assert.Single((await OverviewAsync()).Kinds).Count);

        _clock.Advance(TimeSpan.FromDays(31));
        await PruneAsync();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        AnalyticsDbContext db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
        Assert.Empty(await db.ErrorKinds.ToListAsync(Cancellation));
        Assert.Empty(await db.ErrorReports.ToListAsync(Cancellation));
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private DiagnosticsReport Report(string version, string build, params ErrorOccurrence[] errors) => new()
    {
        Context = new ReportContext { Origin = ReportOrigin.App, Platform = DevicePlatform.Ios, AppVersion = version, AppBuild = build, OsMajor = 26 },
        Errors = [.. errors],
    };

    private ErrorOccurrence Crash(string type = "System.NullReferenceException", int? day = null) => new()
    {
        Day = day ?? Today,
        ExceptionType = type,
        Frames = ["GSBC.ChurchApp.Features.Sermons.Client.Player.Start", "GSBC.ChurchApp.Features.Sermons.Client.SermonPage.PlayAsync"],
        Route = "/sermons/{id}",
        Count = 1,
    };

    private async Task AcceptAsync(DiagnosticsReport report, string? requestId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DiagnosticsInbox>().AcceptAsync(report, requestId, Cancellation);
    }

    private async Task<HealthOverview> OverviewAsync()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<HealthService>().GetOverviewAsync(new HealthQuery { Days = 30 });
    }

    private async Task PruneAsync()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<AnalyticsRetention>().PruneAsync(Cancellation));
    }

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    private sealed class MovableClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class RecordingEmail : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class Holders(IReadOnlyList<Guid> holders) : IPermissionHolders
    {
        public ValueTask<IReadOnlyList<Guid>> HoldersOfAsync(string node, CancellationToken cancellationToken) => ValueTask.FromResult(holders);
    }

    private sealed class Emails(Dictionary<Guid, string> emails) : IAccountEmails
    {
        public ValueTask<IReadOnlyList<string>> EmailsOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<string>>(userIds.Where(emails.ContainsKey).Select(id => emails[id]).ToList());
    }
}
