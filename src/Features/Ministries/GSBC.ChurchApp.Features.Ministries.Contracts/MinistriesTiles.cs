using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class MinistriesTiles
{
    public static readonly HubTileKind Ministries = new("hub.ministries", "Ministries",
        "Your ministries and what's new in them, or finding one: kids, youth, music and more", "Ministries",
        new TileDefaults("Ministries", "Kids, youth, music and more", Tone: TileTones.LiveSoft, Icon: "users", Link: MinistriesDestinations.Directory.Path,
            TitleLabel: "Title for people in no ministry"));

    public static readonly HubTileKind LifeGroups = new("hub.life-groups", "Life Groups",
        "Your Life Group this Sunday, or the next groups to choose from", "Life Groups",
        new TileDefaults("Life Groups", "Sundays at 9:00", Tone: TileTones.MarkerSoft, Icon: "book-open", Link: MinistriesDestinations.LifeGroups.Path,
            TitleLabel: "Title for people not in a group"));

    public static SurfaceSet Surfaces => new([Ministries.Surface, LifeGroups.Surface]);
}
