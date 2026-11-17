namespace GSBC.ChurchApp.Platform.Contracts.Destinations;

public static class WebsiteHosts
{
    private const string AppLabel = "app";
    private const string WebsiteLabel = "embed";

    public static string? OriginFor(Uri app)
    {
        string[] labels = app.Host.Split('.');
        if (labels.Length < 3)
            return null;
        string first = labels[0];
        string? website = first == AppLabel ? WebsiteLabel
            : first.EndsWith($"-{AppLabel}", StringComparison.Ordinal) ? first[..^AppLabel.Length] + WebsiteLabel
            : null;
        return website is null ? null : $"https://{string.Join('.', [website, .. labels[1..]])}";
    }
}
