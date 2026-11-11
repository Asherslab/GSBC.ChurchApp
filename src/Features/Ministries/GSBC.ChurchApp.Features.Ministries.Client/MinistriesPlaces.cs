using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistriesPlaces
{
    public const string About = "";
    public const string News = "news";
    public const string Calendar = "calendar";
    public const string Files = "files";
    public const string Roster = "roster";
    public const string Sundays = "sundays";
    public const string People = "people";
    public const string Settings = "settings";

    public static string Directory => MinistriesDestinations.Directory.Path;

    public static string LifeGroups => MinistriesDestinations.LifeGroups.Path;

    public static string Of(string ministryId, string area = About) =>
        area.Length == 0 ? MinistriesDestinations.Ministry.PathOf(ministryId) : $"{MinistriesDestinations.Ministry.PathOf(ministryId)}/{area}";

}
