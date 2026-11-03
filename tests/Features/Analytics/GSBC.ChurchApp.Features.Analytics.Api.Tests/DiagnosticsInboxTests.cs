using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

public sealed class DiagnosticsInboxTests : IAsyncLifetime
{
    private readonly AnalyticsHost _host = new();

    private int Today => _host.Today.DayNumber;

    public async ValueTask InitializeAsync() => await _host.StartAsync();

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

        Assert.Equal(2, _host.Email.Sent.Count);
        Assert.All(_host.Email.Sent, sent => Assert.Equal("owner@example.org", sent.To));
        Assert.Contains($"https://uat-app.baptist.com.au/health/{nullReference.Id}", _host.Email.Sent[0].Body);
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
        await _host.WithAsync<DiagnosticsInbox>(async inbox =>
        {
            await inbox.RecordServerFailureAsync("/gsbc.sync.Sync/Pull", Grpc.Core.StatusCode.Internal, null, Cancellation);
            await inbox.RecordServerFailureAsync("/gsbc.sync.Sync/Pull", Grpc.Core.StatusCode.Unknown, new InvalidOperationException("Sam's email is sam@example.org"), Cancellation);
        });

        HealthOverview overview = await OverviewAsync();

        Assert.Equal(3, Assert.Single(overview.FailedCalls, call => call.Origin == ReportOrigin.App).Count);
        Assert.Equal(1, Assert.Single(overview.FailedCalls, call => call.Origin == ReportOrigin.Server && call.Status == 13).Count);
        ErrorKindSummary server = Assert.Single(overview.Kinds);
        Assert.Equal(ReportOrigin.Server, server.Origin);
        List<string> kinds = await _host.WithAsync<AnalyticsDbContext, List<string>>(db => db.ErrorKinds
            .Select(kind => kind.ExceptionType + string.Join(' ', kind.Frames) + kind.Where).ToListAsync(Cancellation));
        string stored = string.Join(' ', kinds);
        Assert.DoesNotContain("sam@example.org", stored);
    }

    [Fact]
    public async Task ErrorsGoAfterThirtyDaysAndAResentReportIdIsForgottenAfterSeven()
    {
        await AcceptAsync(Report("1.0", "41", Crash()), requestId: "phone-a-1");

        _host.Clock.Advance(TimeSpan.FromDays(8));
        await PruneAsync();
        await AcceptAsync(Report("1.0", "41", Crash()), requestId: "phone-a-1");
        Assert.Equal(2, Assert.Single((await OverviewAsync()).Kinds).Count);

        _host.Clock.Advance(TimeSpan.FromDays(31));
        await PruneAsync();
        await _host.WithAsync<AnalyticsDbContext>(async db =>
        {
            Assert.Empty(await db.ErrorKinds.ToListAsync(Cancellation));
            Assert.Empty(await db.ErrorReports.ToListAsync(Cancellation));
        });
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

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

    private Task AcceptAsync(DiagnosticsReport report, string? requestId) =>
        _host.WithAsync<DiagnosticsInbox>(inbox => inbox.AcceptAsync(report, requestId, Cancellation));

    private Task<HealthOverview> OverviewAsync() =>
        _host.WithAsync<HealthService, HealthOverview>(health => health.GetOverviewAsync(new HealthQuery { Days = 30 }).AsTask());

    private async Task PruneAsync() =>
        Assert.True(await _host.WithAsync<AnalyticsRetention, bool>(retention => retention.PruneAsync(Cancellation)));

    private static CancellationToken Cancellation => AnalyticsHost.Cancellation;
}
