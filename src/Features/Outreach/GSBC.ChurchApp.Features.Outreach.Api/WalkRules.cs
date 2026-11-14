using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed record WalkVerdict(bool Counts, string Why)
{
    public static readonly WalkVerdict Fine = new(true, "");

    public static WalkVerdict Doubtful(string why) => new(false, why);
}

internal static class WalkRules
{
    public const int MostStretchesPerPiece = 300;
    public const int MostPiecesPerWalk = 2000;
    public static readonly TimeSpan ClockSlack = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan OldestPiece = TimeSpan.FromDays(7);
    private static readonly TimeSpan ShortestElapsed = TimeSpan.FromMinutes(2);
    private const double StreetSlackShare = 1.25;
    private const int StreetSlackMetres = 150;

    public static WalkVerdict Check(WalkRecord walk, WalkPiece piece, OutreachSettings settings, DateTime now)
    {
        if (piece.At > now + ClockSlack || piece.StartedAt > piece.At + ClockSlack)
            return WalkVerdict.Doubtful("Its times are in the future");
        if (piece.At < now - OldestPiece)
            return WalkVerdict.Doubtful("It arrived more than a week after it was walked");
        if (piece.Metres < 0 || piece.Covered.Count > MostStretchesPerPiece)
            return WalkVerdict.Doubtful("It names more streets than one walk can");

        int metres = walk.Metres + piece.Metres;
        if (metres > settings.LongestWalkKm * 1000)
            return WalkVerdict.Doubtful($"It's longer than {settings.LongestWalkKm} km");

        TimeSpan elapsed = piece.At - walk.StartedAt;
        double hours = (elapsed < ShortestElapsed ? ShortestElapsed : elapsed).TotalHours;
        if (metres / 1000.0 / hours > settings.FastestKmh)
            return WalkVerdict.Doubtful($"It's faster than {settings.FastestKmh.ToString("0.#", CultureInfo.InvariantCulture)} km/h");

        int streetMetres = piece.Covered.Sum(stretch => stretch.Metres);
        if (streetMetres > piece.Metres * StreetSlackShare + StreetSlackMetres)
            return WalkVerdict.Doubtful("It marks more street done than was walked");

        return WalkVerdict.Fine;
    }
}
