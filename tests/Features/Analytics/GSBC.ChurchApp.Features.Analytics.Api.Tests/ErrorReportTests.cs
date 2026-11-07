using Grpc.Core;
using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Email;
using Microsoft.Extensions.Configuration;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

public sealed class ErrorReportTests
{
    private const string Pull = "/gsbc.sync.Sync/Pull";
    private static readonly DateOnly Today = new(2026, 11, 2);

    [Fact]
    public void OnlyTheAppAndTheAdminPanelMayReportAndOnlyAboutRecentDays()
    {
        DiagnosticsReport claimingToBeTheServer = Report(ReportOrigin.Server, Crash(Today));
        DiagnosticsReport fromLastYear = Report(ReportOrigin.App, Crash(Today.AddDays(-365)));
        fromLastYear.FailedCalls.Add(new FailedCallCount { Day = Today.DayNumber - 365, Method = Pull, Status = 14, Count = 3 });

        Assert.Null(DiagnosticsReading.Read(claimingToBeTheServer, Today));
        DiagnosticsLines read = DiagnosticsReading.Read(fromLastYear, Today)!;
        Assert.Empty(read.Sightings);
        Assert.Empty(read.FailedCalls);
        Assert.NotNull(DiagnosticsReading.Read(Report(ReportOrigin.Admin, Crash(Today)), Today));
    }

    [Fact]
    public void TheSameErrorOnAnyPhoneOrBuildIsOneKindAndADifferentExceptionIsAnother()
    {
        string onePhone = Fingerprint(Report(ReportOrigin.App, Crash(Today), build: "41"));

        Assert.Equal(onePhone, Fingerprint(Report(ReportOrigin.App, Crash(Today.AddDays(-1)), build: "42")));
        Assert.NotEqual(onePhone, Fingerprint(Report(ReportOrigin.App, Crash(Today, "System.InvalidOperationException"), build: "41")));
    }

    [Fact]
    public void AServerFailureKeepsNoWordsFromItsMessage()
    {
        (FailedCallLine call, ErrorSighting? sighting) = DiagnosticsReading.ServerFailure(Pull, StatusCode.Unknown,
            new InvalidOperationException("Sam's email is sam@example.org"), Today);

        Assert.Equal((ReportOrigin.Server, (int)StatusCode.Unknown), (call.Origin, call.Status));
        Assert.DoesNotContain("sam@example.org", string.Join(' ', [sighting!.ExceptionType, sighting.Route, .. sighting.Frames]));
    }

    [Fact]
    public void ANewKindsEmailNamesTheEnvironmentAndLinksToItOnAppHealth()
    {
        AnalyticsEnvironment uat = new(new ConfigurationBuilder().AddInMemoryCollection([new("Accounts:SiteUrl", "https://uat-app.baptist.com.au/")]).Build());
        ErrorKindRecord kind = new() { Id = Guid.CreateVersion7(), Fingerprint = "", ExceptionType = "System.NullReferenceException", Where = "Player.Start" };

        EmailMessage email = NewErrorKinds.Describe(kind, "owner@example.org", uat);

        Assert.Equal("owner@example.org", email.To);
        Assert.Equal("New error on uat-app.baptist.com.au: NullReferenceException", email.Subject);
        Assert.Contains($"https://uat-app.baptist.com.au/health/{kind.Id}", email.Body);
    }

    private static string Fingerprint(DiagnosticsReport report) => DiagnosticsReading.Fingerprint(Assert.Single(DiagnosticsReading.Read(report, Today)!.Sightings));

    private static DiagnosticsReport Report(ReportOrigin origin, ErrorOccurrence error, string build = "41") => new()
    {
        Context = new ReportContext { Origin = origin, Platform = DevicePlatform.Ios, AppVersion = "1.0", AppBuild = build, OsMajor = 26 },
        Errors = [error],
    };

    private static ErrorOccurrence Crash(DateOnly day, string type = "System.NullReferenceException") => new()
    {
        Day = day.DayNumber,
        ExceptionType = type,
        Frames = ["GSBC.ChurchApp.Features.Sermons.Client.Player.Start", "GSBC.ChurchApp.Features.Sermons.Client.SermonPage.PlayAsync"],
        Route = "/sermons/{id}",
        Count = 1,
    };
}
