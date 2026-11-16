using GSBC.ChurchApp.Platform.Icons;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventsTiles
{
    public const string Agenda = "agenda";
    public const string JustALink = "link";
    private const string Headline = "headline";
    private const string RetiredBulletin = "hub.bulletin";

    public static readonly NumberSetting Entries = new("entries", "Number of events shown", 2, 1, 4);

    public static readonly HubTileKind Week = new("hub.week", "This week",
        "Coming up at church: one-off events, special dates and dates that aren't on.", "the calendar",
        new TileDefaults("This week", Columns: 2, Tone: TileTones.Raised, Link: EventsDestinations.Week.Path, Text: "Calendar",
            TextLabel: "Link text"),
        more: [Entries], variants:
        [
            new(Agenda, "Agenda", "Shows the next event when small, two or three when one row tall, and more when taller.",
                TileVariant.AnySize, (_, _) => true),
            new(JustALink, "Link only", "Shows the icon and title, with a count when there is one.",
                TileVariant.AnySize, (_, _) => false),
            .. TileVariants.Pictures,
        ]);

    public static readonly HubTileKind WhatsNext = new("hub.whats-next", "What's next",
        "The weekly digest: the first thing coming up as the headline and what follows, opening What's next", "the weekly digest",
        new TileDefaults("What's next", Rows: 2, Tone: TileTones.Masthead, Icon: AppIcons.WeeklyDigest, Link: EventsDestinations.WhatsNext.Path,
            Text: "What's next", TitleLabel: "Title when small", TextLabel: "Words to open it"),
        variants:
        [
            new(Headline, "Headline", "The first thing as the headline and what follows under it; how many when small",
                TileVariant.AnySize, (_, _) => true),
            new(JustALink, "Just a link", "The icon and the title, with how many things are coming up",
                TileVariant.AnySize, (_, _) => false),
            .. TileVariants.Pictures,
        ],
        replaces: new HubTileReplacement(RetiredBulletin));

    public static SurfaceSet Surfaces => new([Week.Surface, WhatsNext.Surface]);
}
