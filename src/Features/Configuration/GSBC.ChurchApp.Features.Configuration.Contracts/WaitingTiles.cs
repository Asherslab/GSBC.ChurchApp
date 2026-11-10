using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class WaitingTiles
{
    public static class Layouts
    {
        public const string Poster = "poster";
        public const string Theme = "theme";
        public const string Verse = "verse";
        public const string JustALink = "link";
        public const string Invitation = "invitation";
    }

    private static readonly TileVariant Link = new(Layouts.JustALink, "Just a link", "The icon and the title, with a count where there is one",
        TileVariant.AnySize, (_, _) => false);

    public static readonly HubTileKind Bulletin = new("hub.bulletin", "Sunday bulletin",
        "This Sunday's date, theme and verse, opening the bulletin", "the bulletin",
        new TileDefaults("Read the bulletin", Rows: 2, Tone: TileTones.Masthead, Icon: "bulletin", Link: ComingSoonDestinations.Sunday.Path,
            TitleLabel: "Words to open it", ShowsText: false),
        sample: true, variants:
        [
            new(Layouts.Poster, "Poster", "The date when small; the theme, the date and the verse with a link to read it when bigger",
                TileVariant.AnySize, (_, _) => true),
            new(Layouts.Theme, "Theme first", "The year's theme is the title at every size",
                TileVariant.AnySize, (_, _) => false),
            new(Layouts.Verse, "Verse", "This week's verse in large type, the date small",
                TileVariant.NotSmall, (_, _) => false),
            .. TileVariants.Pictures,
        ]);

    public static readonly HubTileKind Prayer = new("hub.prayer", "Prayer",
        "Asks for prayer requests and opens the prayer wall", "Prayer",
        new TileDefaults("How can we pray for you?", "Share a request with the pastoral team", Tone: TileTones.LiveSoft, Icon: "prayer",
            Link: ComingSoonDestinations.Prayer.Path),
        sample: true, offered: false);

    public static readonly HubTileKind Outreach = new("hub.outreach", "Outreach day",
        "Invites people to letterbox drops, and goes live while outreach is on", "Outreach",
        new TileDefaults("Got an hour?", "Walk from your door: a loop of letterbox drops", Columns: 2, Tone: TileTones.MarkerSoft, Icon: "footprints",
            Link: ComingSoonDestinations.Outreach.Path, TitleLabel: "Title when outreach isn't on"),
        sample: true, variants:
        [
            new(Layouts.Invitation, "Invitation", "The invitation and how to join; a row one row tall, a poster with a go line when taller",
                TileVariant.AnySize, (_, _) => true),
            Link,
            .. TileVariants.Pictures,
        ]);

    public static readonly HubTileKind Give = new("hub.give", "Give",
        "Opens giving online", "Giving",
        new TileDefaults("Give", "Online at baptist.com.au", Icon: "heart", Link: "https://baptist.com.au/give"), offered: false);

    public static readonly HubTileKind Bible = new("hub.bible", "Bible",
        "Opens the Bible", "the Bible",
        new TileDefaults("Bible", "King James Version", Icon: "book-open", Link: "https://www.biblegateway.com/versions/King-James-Version-KJV-Bible/"), offered: false);

    public static readonly HubTileKind Visit = new("hub.visit", "Visit us",
        "When and where the church meets", "church details",
        new TileDefaults("Visit us", "Sundays 10:30am and 6pm · 185 Old Northern Road, Albany Creek", Columns: 2, Tone: TileTones.Raised, Icon: "map-pin",
            Link: "https://maps.apple.com/?q=185+Old+Northern+Road,+Albany+Creek+QLD+4035"), offered: false);

    public static readonly IReadOnlyList<HubTileKind> All = [Bulletin, Outreach, Prayer, Give, Bible, Visit];

    public static SurfaceSet Surfaces => new(All.Select(kind => kind.Surface).ToList());
}
