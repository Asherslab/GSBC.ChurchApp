using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

[Integration("ON CONFLICT folds the same error into one kind and failed calls into one count, RETURNING tells a new kind apart, and a resent report id counts once")]
public sealed class DiagnosticsInboxTests : IAsyncLifetime
{
    private const string Pull = "/gsbc.sync.Sync/Pull";

    private readonly AnalyticsHost _host = new();

    private int Today => _host.Today.DayNumber;

    public async ValueTask InitializeAsync() => await _host.StartAsync();

    [Fact]
    public async Task TheSameErrorFromManyPhonesIsOneKindAResentReportCountsOnceAndTheOwnerHearsOnce()
    {
        await AcceptAsync(Report(Crash()), requestId: "phone-a-1");
        await AcceptAsync(Report(Crash()), requestId: "phone-a-1");
        await AcceptAsync(Report(Crash()), requestId: "phone-b-1");
        await AcceptAsync(Report(Crash(type: "System.InvalidOperationException")), requestId: "phone-b-2");

        HealthOverview overview = await OverviewAsync();

        Assert.Equal(2, overview.Kinds.Count);
        Assert.Equal(2, Assert.Single(overview.Kinds, kind => kind.ExceptionType == "System.NullReferenceException").Count);
        Assert.Equal(2, _host.Email.Sent.Count);
    }

    [Fact]
    public async Task FailedCallsAddUpPerOriginMethodAndStatus()
    {
        await AcceptAsync(Report(failedCalls: 3), requestId: "phone-a-1");
        await AcceptAsync(Report(failedCalls: 2), requestId: "phone-b-1");
        await _host.WithAsync<DiagnosticsInbox>(inbox => inbox.RecordServerFailureAsync(Pull, Grpc.Core.StatusCode.Internal, null, AnalyticsHost.Cancellation));

        HealthOverview overview = await OverviewAsync();

        Assert.Equal(5, Assert.Single(overview.FailedCalls, call => call.Origin == ReportOrigin.App).Count);
        Assert.Equal(1, Assert.Single(overview.FailedCalls, call => call.Origin == ReportOrigin.Server).Count);
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private DiagnosticsReport Report(ErrorOccurrence? error = null, int failedCalls = 0)
    {
        DiagnosticsReport report = new()
        {
            Context = new ReportContext { Origin = ReportOrigin.App, Platform = DevicePlatform.Ios, AppVersion = "1.0", AppBuild = "41", OsMajor = 26 },
        };
        if (error is not null)
            report.Errors.Add(error);
        if (failedCalls > 0)
            report.FailedCalls.Add(new FailedCallCount { Day = Today, Method = Pull, Status = 13, Count = failedCalls });
        return report;
    }

    private ErrorOccurrence Crash(string type = "System.NullReferenceException") => new()
    {
        Day = Today,
        ExceptionType = type,
        Frames = ["GSBC.ChurchApp.Features.Sermons.Client.Player.Start"],
        Count = 1,
    };

    private Task AcceptAsync(DiagnosticsReport report, string requestId) =>
        _host.WithAsync<DiagnosticsInbox>(inbox => inbox.AcceptAsync(report, requestId, AnalyticsHost.Cancellation));

    private Task<HealthOverview> OverviewAsync() =>
        _host.WithAsync<HealthService, HealthOverview>(health => health.GetOverviewAsync(new HealthQuery { Days = 30 }).AsTask());
}
