using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Events.Admin;

public sealed record DigestPickGroup(string Heading, IReadOnlyList<DigestItem> Items);

internal static class DigestDeskWords
{
    private const string OwnIcon = "pencil";

    public static string? OnlyFor(DigestItem item) => item switch
    {
        { ForEveryone: true } or { Kind: DigestItemKind.Own } => null,
        { Kind: DigestItemKind.News } => item.OnlyFor,
        _ => $"Only for {item.CalendarName}",
    };

    public static string WhoFor(DigestItem item) =>
        item.Kind == DigestItemKind.News ? item.OnlyFor.Replace("Only for ", "", StringComparison.Ordinal) : item.CalendarName;

    public static string Source(DigestItem item) => item.Kind switch
    {
        DigestItemKind.News => "Church news",
        DigestItemKind.Own => "Written for the digest",
        _ => item.CalendarName,
    };

    public static string SourceIcon(DigestItem item) => item.Kind switch
    {
        DigestItemKind.News => AppIcons.News,
        DigestItemKind.Own => OwnIcon,
        _ => AppIcons.Calendar,
    };

    public static string Posted(DigestItem item) =>
        EventDates.Parse(item.Posted) is { } posted ? $"Posted {EventWords.LongDate(posted)}" : "";

    public static IReadOnlyList<DigestPickGroup> ByDay(IEnumerable<DigestItem> items) =>
        items.GroupBy(item => item.FirstDate)
            .OrderBy(day => day.Key, StringComparer.Ordinal)
            .Select(day => new DigestPickGroup(EventDates.Parse(day.Key) is { } date ? EventWords.LongDate(date) : "Dates", day.ToList()))
            .ToList();
}
