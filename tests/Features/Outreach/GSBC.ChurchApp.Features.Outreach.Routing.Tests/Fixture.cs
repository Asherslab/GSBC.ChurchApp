using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

internal static class Fixture
{
    private static readonly Lazy<byte[]> Bytes = new(() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "albany-creek.streets")));

    public static GeoPoint Church => new(-27.34335, 152.96467);

    public static StreetGraph AlbanyCreek() => StreetGraphFile.Read(Bytes.Value);

    public static byte[] AlbanyCreekBytes => Bytes.Value;
}
