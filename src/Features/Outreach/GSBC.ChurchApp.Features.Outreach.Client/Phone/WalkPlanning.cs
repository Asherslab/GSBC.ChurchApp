using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using RoutingPoint = GSBC.ChurchApp.Features.Outreach.Routing.GeoPoint;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record WalkAsk(WalkBudget Budget, RoutingPoint Start, IReadOnlyList<string> Campaigns, WalkMode Mode, bool TakeHop = false);

public sealed record PlannedWalk(
    WalkAsk Ask,
    OutreachRegion Region,
    StreetGraph Graph,
    WalkPlanner Planner,
    WalkPlan Plan,
    WalkReach Reach,
    RouteSummary Summary)
{
    public bool IsEmpty => Plan.IsEmpty;
}

public sealed record RouteToWalk(DayRoute Route, OutreachRegion Region, StreetGraph Graph, WalkPlanner Planner, WalkPlan Plan, RouteSummary Summary, double ShareDone);

public static class WalkPlanning
{
    public static IReadOnlyList<string> Carried(OutreachNow now, IReadOnlyList<string> chosen, DateOnly today)
    {
        List<string> active = [.. chosen.Where(id => now.CampaignOf(id) is { } campaign && OutreachRules.IsActive(campaign, today))];
        return active.Count > 0 ? active : [OutreachCampaigns.GeneralId.ToString()];
    }

    public static string KnockingFor(OutreachNow now, string? chosen, IReadOnlyList<string> carried, DateOnly today) =>
        chosen is not null && now.CampaignOf(chosen) is { } campaign && OutreachRules.IsActive(campaign, today) ? chosen : carried[0];

    public static WalkPlanner Planner(OutreachNow now, StreetGraph graph, IReadOnlyList<string> campaigns, WalkMode mode, DateOnly today)
    {
        Openness open = new(
            new WalkNetwork(graph),
            [.. campaigns.Select(campaign => EngineInputs.Coverage(graph, campaign, now.Coverage))],
            today,
            EngineInputs.Settings(now.Settings, campaigns.Count > 0 ? now.CampaignOf(campaigns[0]) : null),
            mode == WalkMode.Doorknock ? WalkKind.Doorknock : WalkKind.Letterbox,
            EngineInputs.Marks(now.Skips, now.Exclusions));
        return new WalkPlanner(open);
    }

    public static PlannedWalk? Plan(OutreachNow now, OutreachRegion region, StreetGraph graph, WalkAsk ask, DateOnly today)
    {
        WalkPlanner planner = Planner(now, graph, ask.Campaigns, ask.Mode, today);
        if (planner.StartNear(ask.Start) is not { } start)
            return null;
        ReachFinder reach = new(planner);
        WalkPlan plan = planner.Plan(start, ask.Budget);
        WalkReach assessed = reach.Assess(start, ask.Budget, plan);
        if (ask.TakeHop && assessed.Hop is { } hop)
        {
            plan = hop.Plan;
            assessed = assessed with { Hop = hop };
        }
        return new PlannedWalk(ask, region, graph, planner, plan, assessed, RouteSummary.Of(plan, planner.Open));
    }

    public static RouteToWalk? Route(OutreachNow now, OutreachRegion region, StreetGraph graph, DayRoute route, OutreachDay day, DateOnly today)
    {
        List<SideStretch> sides = [.. route.Stretches.Select(stretch => EngineInputs.SideStretch(graph, stretch)).OfType<SideStretch>()];
        if (sides.Count == 0)
            return null;
        WalkPlanner planner = Planner(now, graph, [day.Campaign], day.Mode, today);
        WalkNetwork network = planner.Network;
        HashSet<int> pieces = [.. sides.SelectMany(side => network.Split(side.Street, [side.From, side.To])
            .Where(piece => network[piece].From >= side.From - 0.0005 && network[piece].To <= side.To + 0.0005))];
        if (pieces.Count == 0)
            return null;
        int start = planner.NearestEnd(pieces, graph.Projection.ToFlat(EngineInputs.Point(route.Start)));
        WalkPlan plan = planner.Circuit(pieces, start);
        RouteSummary summary = RouteSummary.Of(plan, planner.Open);
        double done = now.Coverage.Count == 0 ? 0 : DoneShare(now, graph, day, sides);
        return new RouteToWalk(route, region, graph, planner, plan, summary, done);
    }

    private static double DoneShare(OutreachNow now, StreetGraph graph, OutreachDay day, IReadOnlyList<SideStretch> sides)
    {
        CampaignCoverage covered = EngineInputs.Coverage(graph, day.Campaign, now.Coverage.Select(street => new StreetCoverage
        {
            Campaign = street.Campaign, Region = street.Region, Street = street.Street, Stretches = [.. street.Stretches.Where(stretch => stretch.At >= day.StartsAt)],
        }));
        int total = 0, done = 0;
        foreach (SideStretch side in sides)
        {
            LetterboxRange boxes = graph.Between(side.Street, side.Side, side.From, side.To);
            for (int box = boxes.Start; box < boxes.End; box++)
            {
                if (!graph.LetterboxKindOf(box).IsHome())
                    continue;
                total++;
                double along = graph.LetterboxAlong(box);
                if (covered.Covered.Any(c => c.Stretch.Street == side.Street && c.Stretch.Side == side.Side && along >= c.Stretch.From && along <= c.Stretch.To))
                    done++;
            }
        }
        return total == 0 ? 0 : done / (double)total;
    }
}
