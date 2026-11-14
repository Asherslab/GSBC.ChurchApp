using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachTiles
{
    public static readonly HubTileKind Outreach = new("hub.outreach", "Outreach",
        "Offers a walk from your door, and leads with the outreach day while one is on", "Outreach",
        new TileDefaults("Got an hour?", "Walk from your door", Tone: TileTones.Sand, Icon: AppIcons.Outreach, Link: OutreachDestinations.Outreach.Path));

    public static SurfaceSet Surfaces => new([Outreach.Surface]);
}
