using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public static class HubSurfaces
{
    public const string EditNode = "config.hub.edit";

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Global, "Arrange the app's Home screen: which tiles show, in what order, for whom and when");

    public const string PublishNode = "config.hub.publish";

    public static readonly Permission Publish =
        new(PublishNode, ScopeKind.Global, "Put changes to the Home screen out to everyone, or on for an event");

    public static readonly SurfaceArea Area = new("home.hub", "Home screen", "The tiles on the app's Home screen", ScopeKind.Global, Edit, Publish);

    public static readonly IReadOnlyList<Choice> TileColours =
    [
        new("plain", "Plain"),
        new("navy", "Navy"),
        new("aqua", "Aqua"),
        new("sand", "Sand"),
    ];

    public static class LinkTile
    {
        public static readonly TextSetting Title = new("title", "Title", required: true, maxLength: 40);
        public static readonly TextSetting Subtitle = new("subtitle", "Subtitle", maxLength: 80);
        public static readonly TextSetting Link = new("link", "Opens", required: true, maxLength: 500, isLink: true,
            help: "The web page the tile opens, starting with https://");
        public static readonly ColourSetting Colour = new("colour", "Colour", TileColours, "plain");
        public static readonly YesNoSetting Wide = new("wide", "Full width");
        public static readonly MediaSetting Image = new("image", "Picture", help: "Shown on the tile, behind its title");

        public static readonly Surface Surface = new("hub.link", Area, "Link tile",
            "A tile that opens a web page: the Bible, a sign-up form, a campaign",
            [Title, Subtitle, Link, Colour, Wide, Image], defaultOrder: 1000, repeatable: true);
    }

    public static readonly SurfaceSet All = new([LinkTile.Surface]);
}
