namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class ConfigurationFeeds
{
    public const string Placements = "config.placements";
    public const string Drafts = "config.drafts";
    public const string EventLayerRecord = "event-layer";

    public static string GroupOf(string area, string? scopeId) => scopeId is null ? area : $"{area}:{scopeId}";
}
