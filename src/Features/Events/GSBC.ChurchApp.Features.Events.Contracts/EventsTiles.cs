using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventsTiles
{
    public const string Agenda = "agenda";
    public const string JustALink = "link";

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

    public static SurfaceSet Surfaces => new([Week.Surface]);
}
