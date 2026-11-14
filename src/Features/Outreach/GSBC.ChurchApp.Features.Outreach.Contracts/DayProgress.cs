namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public readonly record struct CoveredPiece(string Street, StreetSide Side, int From, int To);

public static class DayProgress
{
    public const double DoneShare = 0.9;

    public static RouteProgress Of(DayRoute route, IEnumerable<string> claimedBy, IReadOnlyCollection<CoveredPiece> covered)
    {
        ILookup<(string, StreetSide), CoveredPiece> byStreet = covered.ToLookup(piece => (piece.Street, piece.Side));
        List<Stretch> finished = route.Stretches.Where(stretch => Share(stretch, byStreet[(stretch.Street, stretch.Side)]) >= DoneShare).ToList();
        return new RouteProgress
        {
            RouteId = route.Id,
            ClaimedBy = [.. claimedBy],
            Letterboxes = route.Letterboxes,
            LetterboxesDone = finished.Sum(stretch => stretch.Letterboxes),
            Done = finished,
        };
    }

    public static IEnumerable<CoveredPiece> Since(IEnumerable<StreetCoverage> streets, string campaign, DateTime since) =>
        streets.Where(street => street.Campaign == campaign)
            .SelectMany(street => street.Stretches.Where(stretch => stretch.At >= since).Select(stretch => new CoveredPiece(street.Street, stretch.Side, stretch.From, stretch.To)));

    public static double Share(Stretch stretch, IEnumerable<CoveredPiece> covered)
    {
        int length = stretch.To - stretch.From;
        if (length <= 0)
            return 0;
        List<(int From, int To)> pieces = covered.Where(piece => piece.Street == stretch.Street && piece.Side == stretch.Side)
            .Select(piece => (Math.Max(piece.From, stretch.From), Math.Min(piece.To, stretch.To))).Where(piece => piece.Item1 < piece.Item2)
            .OrderBy(piece => piece.Item1).ToList();
        int done = 0;
        int reached = stretch.From;
        foreach ((int from, int to) in pieces)
        {
            int start = Math.Max(from, reached);
            if (to > start)
                done += to - start;
            reached = Math.Max(reached, to);
        }
        return done / (double)length;
    }
}
