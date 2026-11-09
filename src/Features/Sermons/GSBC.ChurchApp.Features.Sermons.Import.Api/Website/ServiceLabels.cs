using System.Text.RegularExpressions;
using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Website;

internal static partial class ServiceLabels
{
    private static readonly Dictionary<string, string> Days = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sun"] = "Sunday", ["sunday"] = "Sunday",
        ["mon"] = "Monday", ["monday"] = "Monday",
        ["tue"] = "Tuesday", ["tues"] = "Tuesday", ["tuesday"] = "Tuesday",
        ["wed"] = "Wednesday", ["wednesday"] = "Wednesday",
        ["thu"] = "Thursday", ["thur"] = "Thursday", ["thurs"] = "Thursday", ["thursday"] = "Thursday",
        ["fri"] = "Friday", ["friday"] = "Friday",
        ["sat"] = "Saturday", ["saturday"] = "Saturday",
    };

    public static string FromPage(string label, string speaker, DateOnly day) => OldStyle(label, day.DayOfWeek) ?? FromSummary(label, speaker);

    private static string? OldStyle(string label, DayOfWeek day)
    {
        string key = OldStyleKey().Replace(label.ToLowerInvariant(), "");
        return (day, key) switch
        {
            (DayOfWeek.Sunday, "morningservice") => SermonServices.StandardFor("sunday morning"),
            (DayOfWeek.Sunday, "eveningservice" or "sundaynight") => SermonServices.StandardFor("sunday evening"),
            (DayOfWeek.Wednesday, _) when key.StartsWith("midweekservice", StringComparison.Ordinal) => SermonServices.StandardFor("wednesday night"),
            _ => null,
        };
    }

    public static string FromSummary(string summary, string speaker)
    {
        string before = WatchAt().Split(summary)[0];
        string[] parts = before.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "";
        if (SermonServices.StandardFor(Describe(parts[0])) is { } standard)
            return standard;
        IEnumerable<string> extra = parts.Skip(1).Where(part => !NamesTheSpeaker(part, speaker));
        return string.Join(" · ", new[] { Describe(parts[0]) }.Concat(extra));
    }

    private static string Describe(string label)
    {
        string[] words = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !Days.TryGetValue(words[0], out string? day))
            return label.Trim();
        IEnumerable<string> rest = words.Skip(1).Select(word => word.ToUpperInvariant() switch
        {
            "AM" => "morning",
            "PM" => "evening",
            _ when Time().IsMatch(word) => word.ToLowerInvariant(),
            _ => word,
        });
        return string.Join(' ', new[] { day }.Concat(rest));
    }

    private static bool NamesTheSpeaker(string part, string speaker)
    {
        string? surname = speaker.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return surname is { Length: > 1 } && part.Contains(surname, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"[^a-z]+|(?<=mid)\s+(?=week)")]
    private static partial Regex OldStyleKey();

    [GeneratedRegex(@"\s*-?\s*Watch at:?", RegexOptions.IgnoreCase)]
    private static partial Regex WatchAt();

    [GeneratedRegex(@"^\d{1,2}(:\d{2})?(AM|PM)$", RegexOptions.IgnoreCase)]
    private static partial Regex Time();
}
