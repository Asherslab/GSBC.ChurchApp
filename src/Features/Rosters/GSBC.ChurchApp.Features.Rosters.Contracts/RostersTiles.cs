using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Rosters.Contracts;

public static class RostersTiles
{
    public static readonly HubTileKind Roster = new("hub.roster", "Your roster",
        "When you're on next in a ministry, with your answer if one is waiting", "Rosters",
        new TileDefaults("Your roster", "When you're on next", Tone: TileTones.MarkerSoft, Icon: "calendar", Link: "/ministries",
            TitleLabel: "Title for people on no roster"));

    public static SurfaceSet Surfaces => new([Roster.Surface]);
}
