namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class ConfigurationFeeds
{
    public const string Placements = "config.placements";

    public static string GroupOf(string area, string? scopeId) => scopeId is null ? area : $"{area}:{scopeId}";
}
