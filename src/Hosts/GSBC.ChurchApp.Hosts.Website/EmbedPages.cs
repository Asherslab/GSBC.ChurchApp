using GSBC.ChurchApp.Hosts.Website.Components;
using GSBC.ChurchApp.Hosts.Website.Components.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticAssets;

namespace GSBC.ChurchApp.Hosts.Website;

public static class EmbedPages
{
    private const string Label = "label";

    public static IEndpointRouteBuilder MapEmbedPages(this IEndpointRouteBuilder endpoints)
    {
        ResourceAssetCollection assets = Fingerprinted(endpoints);
        endpoints.MapGet("/events", () => Show<EventsPage>()).WithMetadata(assets);
        endpoints.MapGet("/events/{Id}", (string id) => Show<EventPage>(id)).WithMetadata(assets);
        endpoints.MapGet("/sermons", () => Show<SermonsPage>()).WithMetadata(assets);
        endpoints.MapGet("/sermons/series", () => Show<SeriesIndexPage>()).WithMetadata(assets);
        endpoints.MapGet("/sermons/series/{Id}", (string id) => Show<SeriesPage>(id)).WithMetadata(assets);
        endpoints.MapGet("/sermons/speakers", () => Show<SpeakersPage>()).WithMetadata(assets);
        endpoints.MapGet("/sermons/speakers/{Id}", (string id) => Show<SpeakerPage>(id)).WithMetadata(assets);
        endpoints.MapGet("/sermons/{Id}", (string id) => Show<SermonPage>(id)).WithMetadata(assets);
        return endpoints;
    }

    private static ResourceAssetCollection Fingerprinted(IEndpointRouteBuilder endpoints) => new(endpoints.DataSources
        .SelectMany(source => source.Endpoints)
        .Select(endpoint => endpoint.Metadata.GetMetadata<StaticAssetDescriptor>())
        .OfType<StaticAssetDescriptor>()
        .Where(asset => asset.Selectors.Count == 0 && asset.Properties.Any(property => property.Name == Label))
        .DistinctBy(asset => asset.Properties.First(property => property.Name == Label).Value)
        .Select(asset => new ResourceAsset(asset.Route, asset.Properties.Select(property => new ResourceAssetProperty(property.Name, property.Value)).ToList()))
        .ToList());

    private static RazorComponentResult<App> Show<TPage>(string? id = null) => new(new Dictionary<string, object?>
    {
        [nameof(App.Page)] = typeof(TPage),
        [nameof(App.Parameters)] = id is null ? new Dictionary<string, object>() : new Dictionary<string, object> { ["Id"] = id },
    });
}
