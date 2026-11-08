using System.Globalization;
using GSBC.ChurchApp.Features.Sermons.Client;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Hosts.Website;

public static class SeriesWords
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    public static string CountAndMonths(SeriesInfo series) => SermonText.Join(SermonText.Sermons(series.Count), Months(series.FirstAt, series.LastAt));

    public static string Months(DateTime firstUtc, DateTime lastUtc)
    {
        if (firstUtc == default || lastUtc == default)
            return "";
        DateTime first = ChurchTime.ToChurch(firstUtc), last = ChurchTime.ToChurch(lastUtc);
        if (first.Year == last.Year && first.Month == last.Month)
            return last.ToString("MMMM yyyy", English);
        return first.Year == last.Year
            ? $"{first.ToString("MMMM", English)} – {last.ToString("MMMM yyyy", English)}"
            : $"{first.ToString("MMMM yyyy", English)} – {last.ToString("MMMM yyyy", English)}";
    }
}
