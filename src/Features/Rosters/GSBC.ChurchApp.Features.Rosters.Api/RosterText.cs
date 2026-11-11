using System.Globalization;
using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal static class RosterText
{
    public static string Day(DateTime startsAt) => ChurchTime.ToChurch(startsAt).ToString("dddd d MMM", CultureInfo.InvariantCulture);

    public static string When(DateRecord date)
    {
        DateTime local = ChurchTime.ToChurch(date.StartsAt);
        string day = local.ToString("dddd d MMMM", CultureInfo.InvariantCulture);
        return date.AllDay ? day : $"{day}, {Time(local)}";
    }

    public static string Time(DateTime local) =>
        local.ToString(local.Minute == 0 ? "htt" : "h:mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();

    public static string Line(DateRecord date, string position) => $"{position} · {date.Title}, {When(date)}";
}
