using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

internal static class TestSurfaces
{
    public static readonly Permission EditModules = new("ministries.modules.edit", ScopeKind.Ministry, "Arrange a ministry's modules");
    private static readonly Permission PublishModules = new("ministries.modules.publish", ScopeKind.Ministry, "Publish a ministry's modules");
    public static readonly SurfaceArea Modules = new("ministry.modules", "Modules", "", ScopeKind.Ministry, EditModules, PublishModules);
    public static readonly RichTextSetting About = new("about", "About");
    public static readonly Surface News = new("ministry.news", Modules, "News", "", []);
    public static readonly Surface Welcome = new("ministry.welcome", Modules, "Welcome", "", [About]);

    public static readonly PermissionCatalog Permissions = new([new PermissionSet(
        [HubSurfaces.Edit, HubSurfaces.Publish, AppearanceSurfaces.Edit, AppearanceSurfaces.Publish, TabBarSurfaces.Edit, TabBarSurfaces.Publish, EditModules, PublishModules, ConfigurationPermissions.PreviewDraft])]);

    private static readonly DestinationKind Files = new("files", new DestinationArea("files", "Files", "file", 50), "File", "file", "files", OpensOutside: true);
    private static readonly DestinationKind Series = new("series", new DestinationArea("sermons", "Sermons", "sermons", 20), "Series", "sermons", "series");

    public static DestinationCatalog Places => new([ShellDestinations.All,
        new DestinationSet([new DestinationPage("/sermons", "Sermons", "sermons", DestinationAreas.Pages, "A tab", Audience.Everyone)], [Files, Series])]);

    public static SurfaceCatalog Catalog => new([HubSurfaces.All, AppearanceSurfaces.All, TabBarSurfaces.All, new SurfaceSet([News, Welcome]), WaitingTiles.Surfaces]);
}
