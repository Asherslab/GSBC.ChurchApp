using GSBC.ChurchApp.Features.Analytics.Contracts;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal static class HealthWords
{
    public static string Platform(DevicePlatform platform, int osMajor) => platform switch
    {
        DevicePlatform.Ios => osMajor > 0 ? $"iOS {osMajor}" : "iOS",
        DevicePlatform.Android => osMajor > 0 ? $"Android {osMajor}" : "Android",
        DevicePlatform.Web => "A browser",
        _ => "",
    };

    public static string Build(string version, string build, bool development) =>
        version.Length == 0 ? "" : $"{version}{(build.Length > 0 ? $" ({build})" : "")}{(development ? ", a development build" : "")}";
}
