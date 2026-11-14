using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal static class CoverageRules
{
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(366);

    public static DateTime RestStarts(DateTime now, int restDays) => now - TimeSpan.FromDays(Math.Max(1, restDays));

    public static bool Counts(CoverageRecord coverage, DateTime restStarts) => coverage.ExpiredAt is null && coverage.At > restStarts;

    public static List<CoveredStretch> Merge(IEnumerable<CoverageRecord> coverage)
    {
        List<CoveredStretch> merged = [];
        foreach (IGrouping<StreetSide, CoverageRecord> side in coverage.GroupBy(row => row.Side))
        {
            List<(int From, int To)> taken = [];
            foreach (CoverageRecord row in side.OrderByDescending(row => row.At))
                foreach ((int from, int to) in Uncovered(taken, row.From, row.To))
                {
                    merged.Add(new CoveredStretch { Side = side.Key, From = from, To = to, At = row.At, Source = row.Source });
                    taken.Add((from, to));
                }
        }
        return Joined(merged.OrderBy(stretch => stretch.Side).ThenBy(stretch => stretch.From).ToList());
    }

    private static IEnumerable<(int From, int To)> Uncovered(List<(int From, int To)> taken, int from, int to)
    {
        int at = from;
        foreach ((int takenFrom, int takenTo) in taken.Where(piece => piece.To > from && piece.From < to).OrderBy(piece => piece.From))
        {
            if (takenFrom > at)
                yield return (at, takenFrom);
            at = Math.Max(at, takenTo);
        }
        if (at < to)
            yield return (at, to);
    }

    private static List<CoveredStretch> Joined(List<CoveredStretch> ordered)
    {
        List<CoveredStretch> joined = [];
        foreach (CoveredStretch stretch in ordered)
        {
            if (joined.LastOrDefault() is { } last && last.Side == stretch.Side && last.To == stretch.From && last.At == stretch.At && last.Source == stretch.Source)
                last.To = stretch.To;
            else
                joined.Add(stretch);
        }
        return joined;
    }
}
