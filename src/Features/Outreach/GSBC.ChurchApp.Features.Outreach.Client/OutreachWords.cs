using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Outreach.Client;

public static class OutreachWords
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;
    private const char Together = '\u00a0';

    public static string When(Campaign campaign) =>
        campaign.General ? "Always on"
        : (OutreachRules.DateOf(campaign.StartsOn), OutreachRules.DateOf(campaign.EndsOn)) switch
        {
            ({ } starts, { } ends) when starts.Year == ends.Year => $"{Kept(starts.ToString("d MMM", English))} to {Day(ends)}",
            ({ } starts, { } ends) => $"{Day(starts)} to {Day(ends)}",
            ({ } starts, null) => $"From {Day(starts)}",
            (null, { } ends) => $"Until {Day(ends)}",
            _ => "No dates",
        };

    public static string Day(DateOnly day) => Kept(day.ToString("d MMM yyyy", English));

    private static string Kept(string words) => words.Replace(' ', Together);

    public static string Count(int count) => count.ToString("N0", English);

    public static string Kilometres(int metres) => (metres / 1000.0).ToString(metres < 10_000 ? "0.#" : "N0", English) + " km";

    public static string Plural(int count, string one, string many) => $"{Count(count)} {(count == 1 ? one : many)}";

    public static string At(DateTime utc) => ChurchTime.ToChurch(utc).ToString("d MMM, h:mmtt", English).Replace("AM", "am").Replace("PM", "pm");

    public static string Reason(ReportReason reason) => reason switch
    {
        ReportReason.NotAHome => "Not a home",
        ReportReason.Unsafe => "Unsafe",
        ReportReason.Complaint => "A complaint",
        ReportReason.WrongOnTheMap => "Wrong on the map",
        _ => "Something else",
    };

    public static string State(ReportState state) => state switch
    {
        ReportState.Excluded => "Never delivered to now",
        ReportState.Fixed => "Fixed",
        ReportState.Dismissed => "Dismissed",
        _ => "To review",
    };

    public static string Date(DateTime utc) => Day(ChurchTime.DayOf(utc));

    public static string DayTitle(OutreachDay day) => ChurchTime.ToChurch(day.StartsAt).ToString("dddd d MMMM", English);

    public static string Window(OutreachDay day)
    {
        DateTime starts = ChurchTime.ToChurch(day.StartsAt), ends = ChurchTime.ToChurch(day.EndsAt);
        bool samePart = starts.Hour < 12 == ends.Hour < 12;
        return $"{Clock(starts, !samePart)} to {Clock(ends, true)}";
    }

    public static string Minutes(int minutes) => minutes < 60 ? $"{minutes}{Together}min" : $"{minutes / 60}{Together}h{(minutes % 60 > 0 ? $" {minutes % 60}{Together}min" : "")}";

    private static string Clock(DateTime local, bool withPart) =>
        local.ToString(local.Minute == 0 ? "h" : "h:mm", English) + (withPart ? (local.Hour < 12 ? "am" : "pm") : "");

    public static string Place(AddressPoint place) => place.Label.Length > 0 ? place.Label : "A place on the map";
}
