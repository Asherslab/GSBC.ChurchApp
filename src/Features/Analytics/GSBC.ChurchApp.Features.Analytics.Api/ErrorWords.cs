using GSBC.ChurchApp.Features.Analytics.Contracts;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal static class ErrorWords
{
    public static string Origin(ReportOrigin origin, bool nativeCrash) => (origin, nativeCrash) switch
    {
        (ReportOrigin.App, true) => "the app (a native crash)",
        (ReportOrigin.App, false) => "the app",
        (ReportOrigin.Admin, _) => "the admin panel",
        (ReportOrigin.Server, _) => "the server",
        _ => "somewhere unknown",
    };
}
