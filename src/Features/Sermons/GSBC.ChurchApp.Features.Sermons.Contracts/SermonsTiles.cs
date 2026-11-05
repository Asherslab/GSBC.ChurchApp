using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonsTiles
{
    public const string Cover = "cover";
    public const string Listening = "listening";

    public static readonly HubTileKind Latest = new("hub.sermon", "Latest sermon",
        "The newest sermon with its picture and a play button", "Sermons",
        new TileDefaults("Sermons", Columns: 2, Rows: 2, Tone: TileTones.Raised, Icon: "sermons", Link: SermonsDestinations.Listen.Path,
            TitleLabel: "Title before a sermon is out", TitleHelp: "The sermon's own title shows once there is one", ShowsText: false),
        variants:
        [
            new(Cover, "Cover first", "Cover and play when small, the title beside the cover one row tall, under it when taller",
                TileVariant.AnySize, (_, _) => true, ShowsPicture: true),
            new(Listening, "Where you're up to", "The sermon you're part-way through, with play and how long is left; the newest otherwise",
                TileVariant.AnySize, (_, _) => false),
            new(TileVariants.PictureBehind, "Picture behind", "The picture fills the tile with the title, the date and play over a shade",
                TileVariant.NotSmall, (_, _) => false, NeedsPicture: true),
        ]);

    public static SurfaceSet Surfaces => new([Latest.Surface]);
}
