using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record BoardRoute(DayRoute Route, RouteProgress Progress, BoardState State, double Done);

public sealed record PhoneBoard(OutreachDay? Day, Campaign? Campaign, IReadOnlyList<BoardRoute> Routes, bool On, bool Over)
{
    public IReadOnlyList<BoardRoute> Open => Routes.Where(route => route.State == BoardState.Open).ToList();

    public int DonePercent => DayBoards.Percent(Routes.Sum(route => route.Progress.LetterboxesDone), Routes.Sum(route => route.Progress.Letterboxes));

    public int Claimed => Routes.Count(route => route.State != BoardState.Open);
}

public sealed class OutreachDayBoard(OutreachStore store, OutreachClaims claims, TimeProvider time)
{
    public static readonly IReadOnlyList<string> Feeds = OutreachStore.Feeds;

    public async Task<PhoneBoard> LoadAsync(string dayId)
    {
        OutreachNow now = await store.LoadAsync();
        if (now.Days.FirstOrDefault(candidate => candidate.Id == dayId) is not { } day)
            return new PhoneBoard(null, null, [], false, false);
        IReadOnlyList<Claim> all = await claims.WithMineAsync(now);
        List<CoveredPiece> covered = [.. DayProgress.Since(now.Coverage, day.Campaign, day.StartsAt)];
        DateTime utc = time.GetUtcNow().UtcDateTime;
        return new PhoneBoard(day, now.CampaignOf(day.Campaign), Lines(now.RoutesOf(day.Id), all, covered), day.StartsAt <= utc && utc < day.EndsAt, day.EndsAt <= utc);
    }

    public static IReadOnlyList<BoardRoute> Lines(IEnumerable<DayRoute> routes, IReadOnlyList<Claim> claims, IReadOnlyCollection<CoveredPiece> covered) =>
        routes.Select(route =>
            {
                RouteProgress progress = DayProgress.Of(route, claims.Where(claim => claim.RouteId == route.Id).OrderBy(claim => claim.At).Select(claim => claim.FirstName), covered);
                double done = progress.Letterboxes == 0 ? 0 : (double)progress.LetterboxesDone / progress.Letterboxes;
                return new BoardRoute(route, progress, DayBoards.State(progress.ClaimedBy.Count, done), done);
            })
            .OrderBy(line => line.State).ThenBy(line => line.Done).ThenBy(line => line.Route.Number).ToList();
}
