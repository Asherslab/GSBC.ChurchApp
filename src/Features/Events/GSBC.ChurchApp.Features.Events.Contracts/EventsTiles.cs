using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventsTiles
{
    public const string Agenda = "agenda";
    public const string JustALink = "link";

    public static readonly NumberSetting Entries = new("entries", "How many to show", 2, 1, 4);

    public static readonly HubTileKind Week = new("hub.week", "This week",
        "Coming up at church: one-off events, specials and dates that aren't on", "the calendar",
        new TileDefaults("This week", Columns: 2, Tone: TileTones.Raised, Link: EventsDestinations.Week.Path, Text: "Calendar",
            TextLabel: "Words to open the calendar"),
        more: [Entries], variants:
        [
            new(Agenda, "Agenda", "The next thing when small, two or three one row tall, more when taller",
                TileVariant.AnySize, (_, _) => true),
            new(JustALink, "Just a link", "The icon and the title, with a count where there is one",
                TileVariant.AnySize, (_, _) => false),
            .. TileVariants.Pictures,
        ]);

    public static SurfaceSet Surfaces => new([Week.Surface]);
}
