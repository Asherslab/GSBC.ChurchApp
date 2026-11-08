using System.Globalization;
using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using Microsoft.AspNetCore.WebUtilities;

namespace GSBC.ChurchApp.Hosts.Website;

public static class EventLinks
{
    private const string UtcStamp = "yyyyMMdd'T'HHmmss'Z'";
    private const string DayStamp = "yyyyMMdd";

    public static string GoogleCalendar(EventOccurrence occurrence, string pageAddress)
    {
        string dates = occurrence.AllDay
            ? $"{Stamp(EventWords.DayOf(occurrence))}/{Stamp((EventDates.Parse(occurrence.EndDate) ?? EventWords.DayOf(occurrence)).AddDays(1))}"
            : $"{occurrence.StartsAt.ToString(UtcStamp, CultureInfo.InvariantCulture)}/{Later(occurrence).ToString(UtcStamp, CultureInfo.InvariantCulture)}";
        return QueryHelpers.AddQueryString("https://calendar.google.com/calendar/render", new Dictionary<string, string?>
        {
            ["action"] = "TEMPLATE",
            ["text"] = occurrence.Fields.Title,
            ["dates"] = dates,
            ["details"] = pageAddress,
            ["location"] = Address(occurrence) ?? occurrence.Fields.Place?.Name ?? "",
        });
    }

    public static string? Maps(EventOccurrence occurrence) =>
        Address(occurrence) is { } address
            ? QueryHelpers.AddQueryString("https://www.google.com/maps/search/", new Dictionary<string, string?> { ["api"] = "1", ["query"] = address })
            : null;

    private static string? Address(EventOccurrence occurrence) =>
        occurrence.Fields.Place is { Address.Length: > 0 } place ? place.Address : null;

    private static DateTime Later(EventOccurrence occurrence) =>
        occurrence.EndsAt > occurrence.StartsAt ? occurrence.EndsAt : occurrence.StartsAt.AddHours(1);

    private static string Stamp(DateOnly day) => day.ToString(DayStamp, CultureInfo.InvariantCulture);
}
