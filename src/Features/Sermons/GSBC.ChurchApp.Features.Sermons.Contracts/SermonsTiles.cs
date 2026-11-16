using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonsTiles
{
    public const string Cover = "cover";
    public const string Listening = "listening";

    public static readonly HubTileKind Latest = new("hub.sermon", "Latest sermon",
        "The newest sermon, with its picture and a play button.", "Sermons",
        new TileDefaults("Sermons", Columns: 2, Rows: 2, Tone: TileTones.Raised, Icon: "sermons", Link: SermonsDestinations.Listen.Path,
            TitleLabel: "Title until there's a sermon", TitleHelp: "Shown on the tile until the first sermon is in the app. After that, the tile shows the newest sermon's title.", ShowsText: false),
        variants:
        [
            new(Cover, "Cover first", "Small, the tile shows the cover and a play button. One row tall, the title sits beside the cover; taller, the title sits under it.",
                TileVariant.AnySize, (_, _) => true, ShowsPicture: true),
            new(Listening, "Where you're up to", "The sermon you're part-way through, with a play button and the time left. With none in progress, the newest sermon.",
                TileVariant.AnySize, (_, _) => false),
            new(TileVariants.PictureBehind, "Picture behind", "The picture fills the tile, with the title, date and a play button over a shade.",
                TileVariant.NotSmall, (_, _) => false, NeedsPicture: true),
        ]);

    public static SurfaceSet Surfaces => new([Latest.Surface]);
}
