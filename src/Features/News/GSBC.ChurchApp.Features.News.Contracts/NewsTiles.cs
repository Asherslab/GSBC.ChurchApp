using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.News.Contracts;

public static class NewsTiles
{
    private const string FrontPage = "front-page";
    public const string WhatsNew = "whats-new";
    public const string Headlines = "headlines";

    public static readonly HubTileKind News = new("hub.news", "News",
        "Church news: the newest post with its picture, what's new since you last looked, or the latest headlines", "News",
        new TileDefaults("News", Columns: 2, Rows: 2, Tone: TileTones.Raised, Icon: AppIcons.News, Link: NewsDestinations.News.Path,
            TitleLabel: "Title", TitleHelp: "Shown on the tile when there's no news, and above the headlines.", ShowsText: false),
        variants:
        [
            new(FrontPage, "Front page", "The newest post with its picture. At 2 × 2, the post before it shows underneath.",
                TileVariant.AnySize, (_, _) => true),
            new(WhatsNew, "What's new", "How many posts are new since you last opened News, and the newest one.",
                TileVariant.AnySize, (_, _) => false),
            new(Headlines, "Headlines", "The newest headlines, each with when it was posted. At one cell, only the newest.",
                TileVariant.AnySize, (_, _) => false),
        ]);

    public static SurfaceSet Surfaces => new([News.Surface]);
}
