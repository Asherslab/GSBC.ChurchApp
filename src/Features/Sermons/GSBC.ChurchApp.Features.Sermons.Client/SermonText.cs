using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public static class SermonText
{
    public static string Day(DateTime preachedAt) => ChurchTime.ToChurch(preachedAt).ToString("d MMM");

    public static string LongDay(DateTime preachedAt) => ChurchTime.ToChurch(preachedAt).ToString("dddd d MMMM yyyy");

    public static string Length(double seconds) => seconds switch
    {
        <= 0 => "",
        < 3600 => $"{Math.Max(1, (int)Math.Round(seconds / 60))} min",
        _ => $"{(int)(seconds / 3600)} hr {(int)Math.Round(seconds % 3600 / 60)} min",
    };

    public static string Left(ListeningEntry entry) =>
        entry.Duration > 0 ? $"{Length(Math.Max(60, entry.Duration - entry.Position))} left" : "Part-way through";

    public static string Clock(double seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(Math.Max(0, Math.Floor(seconds)));
        return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
    }

    public static string Join(params IEnumerable<string?> parts) => string.Join(" · ", parts.Where(part => !string.IsNullOrEmpty(part)));
}
