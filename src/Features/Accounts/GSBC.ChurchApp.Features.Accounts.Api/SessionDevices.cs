using GSBC.ChurchApp.Features.Accounts.Data;

namespace GSBC.ChurchApp.Features.Accounts.Api;

internal static class SessionDevices
{
    private static readonly (string Marker, string Name)[] Browsers =
    [
        ("Edg", "Edge"),
        ("Firefox/", "Firefox"),
        ("FxiOS/", "Firefox"),
        ("CriOS/", "Chrome"),
        ("Chrome/", "Chrome"),
        ("Safari/", "Safari"),
    ];

    private static readonly (string Marker, string Name)[] Systems =
    [
        ("iPhone", "an iPhone"),
        ("iPad", "an iPad"),
        ("Android", "Android"),
        ("Macintosh", "a Mac"),
        ("Windows", "Windows"),
        ("CrOS", "a Chromebook"),
        ("Linux", "Linux"),
    ];

    public static string Describe(SessionClient client, string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
            return client == SessionClient.Browser ? "A browser" : client.ToString();
        if (client != SessionClient.Browser)
            return deviceName;

        string browser = Find(Browsers, deviceName) ?? "A browser";
        return Find(Systems, deviceName) is { } system ? $"{browser} on {system}" : browser;
    }

    private static string? Find((string Marker, string Name)[] known, string userAgent) =>
        known.FirstOrDefault(entry => userAgent.Contains(entry.Marker, StringComparison.Ordinal)).Name;
}
