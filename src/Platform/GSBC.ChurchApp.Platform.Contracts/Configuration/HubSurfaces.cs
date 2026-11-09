using GSBC.ChurchApp.Platform.Contracts.Analytics;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public static class HubSurfaces
{
    public const string EditNode = "config.hub.edit";

    public static readonly Permission Edit =
        new(EditNode, ScopeKind.Global, "Arrange the app's Home screen: which tiles show, in what order, for whom and when", area: "App");

    public const string PublishNode = "config.hub.publish";

    public static readonly Permission Publish =
        new(PublishNode, ScopeKind.Global, "Put changes to the Home screen out to everyone, or on for an event", area: "App");

    public static readonly Measure TileTapped = new("hub.tile.tapped", "Home screen tiles tapped",
        "Taps on each Home screen tile. One person tapping twice counts twice", SubjectKinds.HubTile, SplitByGroup: true);

    public static readonly SurfaceArea Area = new("home.hub", "Home screen", "The tiles on the app's Home screen", ScopeKind.Global, Edit, Publish);

    private static readonly IReadOnlyList<Choice> TileColours =
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

    public static class Grid
    {
        public static readonly NumberSetting Columns = new("columns", "Columns", HubGrid.FewestColumns, HubGrid.FewestColumns, HubGrid.MostColumns,
            help: "How many columns of tiles Home has on a phone");

        public static readonly Surface Surface = new("hub.grid", Area, "The grid", "How many columns Home's tiles are laid out in",
            [Columns], defaultOrder: -1000);

        public static int ColumnsIn(IEnumerable<ArrangedSurface> shown) =>
            shown.FirstOrDefault(entry => entry.Surface.Key == Surface.Key)?.Values.Get(Columns) ?? HubGrid.FewestColumns;
    }

    public static class Top
    {
        public const string Today = "date";
        public const string Words = "words";
        public const string Nothing = "none";

        public static readonly TextSetting Title = new("title", "Title", "Good Shepherd", required: true, maxLength: 40);

        public static readonly ChoiceSetting Under = new("under", "Under the title",
            [new(Today, "Today's date"), new(Words, "Your own words"), new(Nothing, "Nothing")], Today);

        public static readonly TextSetting Line = new("line", "Your words", maxLength: 80, help: "Shown under the title when Under the title is Your own words");

        public static readonly Surface Surface = new("hub.top", Area, "Top of Home", "The title at the top of Home and the line under it",
            [Title, Under, Line], defaultOrder: -2000);

        public static HomeTop In(IEnumerable<ArrangedSurface> shown) =>
            Of(shown.FirstOrDefault(entry => entry.Surface.Key == Surface.Key)?.Values ?? SettingValues.None);

        public static HomeTop Of(SettingValues values) => new(values.Get(Title), values.Get(Under) switch
        {
            Words when values.Get(Line) is { Length: > 0 } line => new HomeTopLine.Own(line),
            Words or Nothing => HomeTopLine.None,
            _ => HomeTopLine.Date,
        });
    }

    public static class Icon
    {
        public static readonly TextSetting Name = new("name", "Name", "New icon", required: true, maxLength: 40);
        public static readonly SvgIconSetting Drawing = new("svg", "Drawing");

        public static readonly Surface Surface = new("hub.icon", Area, "Icon", "A drawing staff uploaded, to use on tiles",
            [Name, Drawing], defaultOrder: 5000, repeatable: true);

        public static IReadOnlyDictionary<string, string> In(IEnumerable<ArrangedSurface> shown) =>
            shown.Where(entry => entry.Surface.Key == Surface.Key && entry.Placement is not null)
                .ToDictionary(entry => IconSetting.Custom(entry.Placement!.Id), entry => entry.Values.Get(Drawing));
    }

    public static class Saved
    {
        public static readonly TextSetting Name = new("name", "Name", "A saved tile", required: true, maxLength: 60);
        public static readonly KindSetting Kind = new("kind", "Kind of tile");
        public static readonly TemplateSetting Tile = new("tile", "The tile");

        public static readonly Surface Surface = new("hub.saved", Area, "Saved tile", "A tile kept in the library to add again",
            [Name, Kind, Tile], defaultOrder: 6000, repeatable: true, uniqueBy: Name);
    }

    public static readonly HubTileKind PlainTile = new("hub.tile", "Link tile",
        "Your own words, icon, picture and link: Give, the Bible, Visit us, a campaign, a page on the website", "",
        new TileDefaults("A new tile", Icon: "sparkles", TitleRequired: true));

    public static bool IsArrangement(Surface surface) =>
        surface.Key == Grid.Surface.Key || surface.Key == Top.Surface.Key || surface.Key == Icon.Surface.Key || surface.Key == Saved.Surface.Key;

    public static SurfaceSet All => new([LinkTile.Surface, Grid.Surface, Top.Surface, Icon.Surface, Saved.Surface, PlainTile.Surface]);
}

public sealed record HomeTop(string Title, HomeTopLine Line);

public abstract record HomeTopLine
{
    public static readonly HomeTopLine Date = new Today();
    public static readonly HomeTopLine None = new Nothing();

    public sealed record Today : HomeTopLine;

    public sealed record Nothing : HomeTopLine;

    public sealed record Own(string Words) : HomeTopLine;
}
