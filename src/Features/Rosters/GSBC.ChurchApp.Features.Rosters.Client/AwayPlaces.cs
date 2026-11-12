namespace GSBC.ChurchApp.Features.Rosters.Client;

public static class AwayPlaces
{
    public const string Yours = "away";

    public static string Team(string ministryId) => $"ministries/{ministryId}/away";

    public static string Roster(string ministryId) => $"ministries/{ministryId}/roster";
}

public sealed class AwayOnItsOwnPage;
