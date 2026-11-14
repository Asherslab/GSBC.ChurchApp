using GSBC.ChurchApp.Features.Outreach.Client.Phone;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class DayBoardTests
{
    private static readonly DateTime Starts = new(2026, 12, 5, 22, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void RoutesWithNoOneComeFirstAndOnlyWalkingSinceTheDayStartedCounts()
    {
        DayRoute open = Route("open", 1, "1/2/3"), claimed = Route("claimed", 2, "4/5/6"), walked = Route("walked", 3, "7/8/9");
        List<Claim> claims = [new() { RouteId = "claimed", FirstName = "Sam" }, new() { RouteId = "walked", FirstName = "Ruth" }];
        StreetCoverage coverage = new()
        {
            Campaign = "general", Street = "7/8/9",
            Stretches = [new() { Side = StreetSide.Left, From = 0, To = 1000, At = Starts.AddMinutes(40) }],
        };
        StreetCoverage before = new()
        {
            Campaign = "general", Street = "4/5/6",
            Stretches = [new() { Side = StreetSide.Left, From = 0, To = 1000, At = Starts.AddDays(-3) }],
        };

        IReadOnlyList<BoardRoute> lines = OutreachDayBoard.Lines([walked, claimed, open], claims, [.. DayProgress.Since([coverage, before], "general", Starts)]);

        Assert.Equal(["open", "claimed", "walked"], lines.Select(line => line.Route.Id));
        Assert.Equal([BoardState.Open, BoardState.Claimed, BoardState.Done], lines.Select(line => line.State));
    }

    private static DayRoute Route(string id, int number, string street) => new()
    {
        Id = id, Number = number, Letterboxes = 10,
        Stretches = [new() { Region = "r", Street = street, Side = StreetSide.Left, From = 0, To = 1000, Letterboxes = 10, Metres = 100 }],
    };
}
