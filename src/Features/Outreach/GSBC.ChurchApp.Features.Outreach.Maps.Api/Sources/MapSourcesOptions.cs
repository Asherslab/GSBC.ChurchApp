namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;

public sealed class MapSourcesOptions
{
    public const string Section = "Outreach:Maps";

    public Uri Streets { get; set; } = new("https://download.geofabrik.de/australia-oceania/australia/queensland-latest.osm.pbf");
    public Uri Addresses { get; set; } = new("https://spatial-gis.information.qld.gov.au/arcgis/rest/services/PlanningCadastre/LandParcelPropertyFramework/MapServer/0/query");
    public Uri MoretonBayLots { get; set; } = new("https://services-ap1.arcgis.com/152ojN3Ts9H3cdtl/arcgis/rest/services/ZM_Zones_WebMercator_OpenData/FeatureServer/0/query");
    public Uri BrisbaneZones { get; set; } = new("https://services2.arcgis.com/dEKgZETqwmDAh1rP/arcgis/rest/services/Zoning_opendata/FeatureServer/0/query");
    public Uri TileBuilds { get; set; } = new("https://build-metadata.protomaps.dev/builds.json");
    public Uri TileBuildFiles { get; set; } = new("https://build.protomaps.com/");
    public Uri NationalAddresses { get; set; } = new("https://data.gov.au/data/api/3/action/package_show?id=geocoded-national-address-file-g-naf");
    public bool ImportNationalAddresses { get; set; } = true;
    public int MaxZoom { get; set; } = 15;
    public int OverviewMaxZoom { get; set; } = 10;
    public bool SeedChurchArea { get; set; } = true;
    public double ChurchLatitude { get; set; } = -27.34335;
    public double ChurchLongitude { get; set; } = 152.96467;
    public double ChurchRadiusKm { get; set; } = 20;
}
