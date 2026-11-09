using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public static class AnalyticsPermissions
{
    public const string ViewNode = "analytics.view";
    public const string HealthViewNode = "analytics.health.view";
    public const string InsightsViewerRole = "insights-viewer";
    public const string DeveloperRole = "developer";

    public static readonly Permission View =
        new(ViewNode, ScopeKind.Global, "See how the app is used: totals of phones, plays and taps, never anything about a person", area: "Insights and app health");

    public static readonly Permission HealthView =
        new(HealthViewNode, ScopeKind.Global, "See app errors and failed calls, and get an email when a new kind of error appears", area: "Insights and app health");

    public static readonly IReadOnlyList<Permission> All = [View, HealthView];
}
