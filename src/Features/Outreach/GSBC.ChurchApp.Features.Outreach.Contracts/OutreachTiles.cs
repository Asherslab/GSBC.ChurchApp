using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Icons;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachTiles
{
    public const string Invitation = "invitation";
    public const string JustALink = "link";

    public static readonly HubTileKind Outreach = new("hub.outreach", "Outreach",
        "Invites people to walk from their door, and leads with the outreach day while one is on", "Outreach",
        new TileDefaults("Got an hour?", "Walk from your door: a loop of letterbox drops", Columns: 2, Tone: TileTones.MarkerSoft, Icon: AppIcons.Outreach,
            Link: OutreachDestinations.Outreach.Path, TitleLabel: "Title when outreach isn't on"),
        sample: true, variants:
        [
            new(Invitation, "Invitation", "The invitation and how to join; a row one row tall, a poster with a go line when taller; the day while one is on",
                TileVariant.AnySize, (_, _) => true),
            new(JustALink, "Just a link", "The icon and the title", TileVariant.AnySize, (_, _) => false),
            .. TileVariants.Pictures,
        ]);

    public static SurfaceSet Surfaces => new([Outreach.Surface]);
}
