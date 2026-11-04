using System.Text;
using System.Text.Json;

namespace GSBC.ChurchApp.Features.Sermons.Api.Website;

internal static class TithelyArtwork
{
    public static string? OriginalOf(string? resizedUrl, string? fileHost)
    {
        if (resizedUrl is null || fileHost is null || !Uri.TryCreate(resizedUrl, UriKind.Absolute, out Uri? uri))
            return null;
        string[] segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        int job = Array.IndexOf(segments, "df_media");
        if (job < 0 || job + 1 >= segments.Length || PathOf(segments[job + 1]) is not { } path)
            return resizedUrl;
        return $"https://{fileHost}/{string.Join('/', path.Split('/').Select(Uri.EscapeDataString))}";
    }

    private static string? PathOf(string encodedJob)
    {
        try
        {
            string base64 = encodedJob.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            using JsonDocument steps = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
            foreach (JsonElement step in steps.RootElement.EnumerateArray())
                if (step.GetArrayLength() >= 2 && step[0].GetString() == "f")
                    return step[1].GetString();
            return null;
        }
        catch (Exception exception) when (exception is FormatException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}
