using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

public static class PrayerTiles
{
    public static readonly HubTileKind Prayer = new("hub.prayer", "Prayer",
        "Asks for prayer requests and opens the prayer wall", "Prayer",
        new TileDefaults("How can we pray for you?", "Share a request with the pastoral team", Tone: TileTones.LiveSoft, Icon: "prayer",
            Link: PrayerDestinations.Prayer.Path),
        sample: true);

    public static SurfaceSet Surfaces => new([Prayer.Surface]);
}
