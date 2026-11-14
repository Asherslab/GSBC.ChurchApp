namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;

public sealed class MapSourcesOptions
{
    public const string Section = "Outreach:Maps";

    public Uri[] Overpass { get; set; } = [new("https://overpass-api.de/api/interpreter"), new("https://overpass.private.coffee/api/interpreter")];
    public Uri Addresses { get; set; } = new("https://spatial-gis.information.qld.gov.au/arcgis/rest/services/PlanningCadastre/LandParcelPropertyFramework/MapServer/0/query");
    public Uri MoretonBayLots { get; set; } = new("https://services-ap1.arcgis.com/152ojN3Ts9H3cdtl/arcgis/rest/services/ZM_Zones_WebMercator_OpenData/FeatureServer/0/query");
    public Uri BrisbaneZones { get; set; } = new("https://services2.arcgis.com/dEKgZETqwmDAh1rP/arcgis/rest/services/Zoning_opendata/FeatureServer/0/query");
    public Uri TileBuilds { get; set; } = new("https://build-metadata.protomaps.dev/builds.json");
    public Uri TileBuildFiles { get; set; } = new("https://build.protomaps.com/");
    public int MaxZoom { get; set; } = 15;
    public bool SeedChurchArea { get; set; } = true;
    public double ChurchLatitude { get; set; } = -27.34335;
    public double ChurchLongitude { get; set; } = 152.96467;
    public double ChurchRadiusKm { get; set; } = 20;
}
