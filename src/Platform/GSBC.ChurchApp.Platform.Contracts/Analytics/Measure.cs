namespace GSBC.ChurchApp.Platform.Contracts.Analytics;

public sealed record Measure(string Key, string Label, string Counts, string? SubjectKind = null, bool OncePerPhone = false, bool SplitByGroup = false);

public static class SubjectKinds
{
    public const string HubTile = "hub-tile";
}
