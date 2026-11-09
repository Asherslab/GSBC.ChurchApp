using GSBC.ChurchApp.Features.Sermons.Contracts;
using System.Text.RegularExpressions;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public static partial class SermonText
{
    public static string ShortTitle(Sermon sermon)
    {
        if (sermon.SeriesName is not { Length: > 0 } series || !sermon.Title.StartsWith(series, StringComparison.OrdinalIgnoreCase))
            return sermon.Title;
        Match rest = AfterPart().Match(sermon.Title[series.Length..]);
        return rest.Success && rest.Groups["rest"].Value.Trim() is { Length: > 2 } shortened ? shortened : sermon.Title;
    }

    [GeneratedRegex(@"^\s*[:\-–]?\s*Part\s+\d+\s*[:\-–]\s*(?<rest>.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex AfterPart();

    public static string Day(DateTime preachedAt) => ChurchTime.ToChurch(preachedAt).ToString("d\u00a0MMM");

    public static string LongDay(DateTime preachedAt) => ChurchTime.ToChurch(preachedAt).ToString("dddd d MMMM yyyy");

    public static string When(Sermon sermon)
    {
        DateTime day = ChurchTime.ToChurch(sermon.PreachedAt);
        bool serviceNamesTheDay = sermon.Service.StartsWith(day.ToString("dddd"), StringComparison.OrdinalIgnoreCase);
        return Join(day.ToString(serviceNamesTheDay ? "d MMMM yyyy" : "dddd d MMMM yyyy"), sermon.Service);
    }

    public static string Sermons(int count) => count == 1 ? "1 sermon" : $"{count} sermons";

    public static string Month(DateTime at) => ChurchTime.ToChurch(at).ToString("MMM yyyy");

    public static string Length(double seconds) => seconds switch
    {
        <= 0 => "",
        < 3600 => $"{Math.Max(1, (int)Math.Round(seconds / 60))}\u00a0min",
        _ => $"{(int)(seconds / 3600)}\u00a0hr {(int)Math.Round(seconds % 3600 / 60)}\u00a0min",
    };

    public static string Left(ListeningEntry entry) =>
        entry.Duration > 0 ? $"{Length(Math.Max(60, entry.Duration - entry.Position))} left" : "Part-way through";

    public static string Join(params IEnumerable<string?> parts) => string.Join(" · ", parts.Where(part => !string.IsNullOrEmpty(part)));
}
