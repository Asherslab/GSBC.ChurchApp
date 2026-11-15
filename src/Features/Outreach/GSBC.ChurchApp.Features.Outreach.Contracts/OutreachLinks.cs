using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachLinks
{
    public const string Desk = "/outreach";
    public const string FollowUps = "/outreach/follow-ups";
    public const string Reports = "/outreach/reports";
    public const string Days = "/outreach/days";

    public static string DayPlan(string dayId) => $"/outreach/map?day={dayId}";

    public static string DayDesk(string dayId) => $"/outreach/days?day={dayId}";

    public const string Settings = "/outreach/settings";
    public const string NewMapArea = "/outreach/map?job=map-areas&area=new";

    public static string MapArea(string areaId) => $"/outreach/map?job=map-areas&area={areaId}";

    public const string Home = "outreach";
    public const string PlanWalk = "outreach/walk";
    public const string Walking = "outreach/walking";
    public const string Mine = "outreach/mine";
    public const string OfflineMaps = "outreach/offline-maps";

    public static string Day(string dayId) => $"outreach/days/{dayId}";

    public static string DayBoard(string dayId) => $"outreach/days/{dayId}/board";

    public static string Route(string code) => $"outreach/r/{code}";

    public static string RouteLink(Uri appHost, string code) => DestinationPaths.Shareable(appHost, Route(code));
}
