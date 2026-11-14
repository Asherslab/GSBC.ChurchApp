using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal static class PlanRules
{
    private const int MostRoutes = 60;
    private const int MostStretches = 20_000;

    public static string? WhyNot(DayDraft draft) =>
        draft.Area.Count > 0 && !OutreachRules.IsAPolygon(draft.Area) ? "Draw the area with at least three points."
        : !Enum.IsDefined(draft.Target) || draft.PerRoute is < 0 or > 100_000 ? "Choose how big each route should be."
        : WhyNot(draft.Routes.Count, draft.Routes.SelectMany(route => route.Stretches).ToList());

    public static string? WhyNot(IReadOnlyList<DayRoute> routes) =>
        routes.Count == 0 ? "There are no routes to publish."
        : routes.Any(route => route.Stretches.Count == 0) ? "Every route needs its streets."
        : WhyNot(routes.Count, routes.SelectMany(route => route.Stretches).ToList());

    private static string? WhyNot(int routes, IReadOnlyList<Stretch> stretches) =>
        routes > MostRoutes ? $"A day can have up to {MostRoutes} routes."
        : stretches.Count > MostStretches ? "That's more streets than a day can hold. Draw a smaller area."
        : stretches.Any(stretch => !OutreachRules.IsValid(stretch)) ? "A route has a street the map doesn't know."
        : null;
}
