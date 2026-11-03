namespace GSBC.ChurchApp.Platform.Contracts.Analytics;

public sealed record Measure(
    string Key, string Label, string Counts, string? SubjectKind = null, bool OncePerPhone = false, bool SplitByGroup = false,
    IReadOnlyList<MeasureState>? States = null);

public sealed record MeasureState(string Key, string Label);

public static class SubjectKinds
{
    public const string HubTile = "hub-tile";
}
