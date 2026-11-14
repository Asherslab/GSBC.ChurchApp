using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

internal static class Fixture
{
    private static readonly Lazy<StreetGraph> Graph = new(() => StreetGraphFile.Read(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "albany-creek.streets"))));

    public static GeoPoint Church => new(-27.34335, 152.96467);

    public static StreetGraph AlbanyCreek() => Graph.Value;
}
