using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class WaitingTiles
{
    public static readonly HubTileKind Sermon = new("hub.sermon", "Latest sermon",
        "The newest sermon with its picture and a play button", "Sermons",
        new TileDefaults("Sermons", Columns: 2, Rows: 2, Tone: TileTones.Raised, Link: ComingSoonDestinations.Sermons.Path,
            TitleLabel: "Title before a sermon is out", TitleHelp: "The sermon's own title shows once there is one", ShowsText: false),
        sample: true);

    public static readonly HubTileKind Bulletin = new("hub.bulletin", "Sunday bulletin",
        "This Sunday's date, theme and verse, opening the bulletin", "the bulletin",
        new TileDefaults("Read the bulletin", Rows: 2, Tone: TileTones.Masthead, Icon: "bulletin", Link: ComingSoonDestinations.Sunday.Path,
            TitleLabel: "Words to open it", ShowsText: false),
        sample: true);

    public static readonly NumberSetting WeekEntries = new("entries", "How many to show", 2, 1, 4);

    public static readonly HubTileKind Week = new("hub.week", "This week",
        "The next things on at church this week", "the calendar",
        new TileDefaults("This week", Columns: 2, Tone: TileTones.Raised, Link: ComingSoonDestinations.Week.Path, Text: "Calendar",
            TextLabel: "Words to open the calendar"),
        sample: true, more: [WeekEntries]);

    public static readonly HubTileKind Prayer = new("hub.prayer", "Prayer",
        "Asks for prayer requests and opens the prayer wall", "Prayer",
        new TileDefaults("How can we pray for you?", "Share a request with the pastoral team", Tone: TileTones.LiveSoft, Icon: "prayer",
            Link: ComingSoonDestinations.Prayer.Path),
        sample: true);

    public static readonly HubTileKind Outreach = new("hub.outreach", "Outreach day",
        "Invites people to letterbox drops, and goes live while outreach is on", "Outreach",
        new TileDefaults("Got an hour?", "Walk from your door: a loop of letterbox drops", Columns: 2, Icon: "footprints",
            Link: ComingSoonDestinations.Outreach.Path, TitleLabel: "Title when outreach isn't on"),
        sample: true);

    public static readonly HubTileKind LifeGroups = new("hub.life-groups", "Life Groups",
        "Your Life Group, or finding one", "Life Groups",
        new TileDefaults("Life Groups", "Find a group that meets near you", Columns: 2, Icon: "users", Link: ComingSoonDestinations.LifeGroups.Path,
            TitleLabel: "Title for people not in a group"),
        sample: true);

    public static readonly HubTileKind Ministries = new("hub.ministries", "Ministries",
        "The church's ministries: kids, youth, music and more", "Ministries",
        new TileDefaults("Ministries", "Kids, youth, music and more", Columns: 2, Icon: "sparkles", Link: ComingSoonDestinations.Ministries.Path),
        sample: true);

    public static readonly HubTileKind Give = new("hub.give", "Give",
        "Opens giving online", "Giving",
        new TileDefaults("Give", "Online at baptist.com.au", Icon: "heart", Link: "https://baptist.com.au/give"));

    public static readonly HubTileKind Bible = new("hub.bible", "Bible",
        "Opens the Bible", "the Bible",
        new TileDefaults("Bible", "King James Version", Icon: "book-open", Link: "https://www.biblegateway.com/versions/King-James-Version-KJV-Bible/"));

    public static readonly HubTileKind Visit = new("hub.visit", "Visit us",
        "When and where the church meets", "church details",
        new TileDefaults("Visit us", "Sundays 10:30am and 6pm · 185 Old Northern Road, Albany Creek", Columns: 2, Icon: "map-pin",
            Link: "https://maps.apple.com/?q=185+Old+Northern+Road,+Albany+Creek+QLD+4035"));

    public static readonly IReadOnlyList<HubTileKind> All = [Sermon, Bulletin, Week, Prayer, Give, Outreach, Bible, LifeGroups, Ministries, Visit];

    public static SurfaceSet Surfaces => new(All.Select(kind => kind.Surface).ToList());
}
