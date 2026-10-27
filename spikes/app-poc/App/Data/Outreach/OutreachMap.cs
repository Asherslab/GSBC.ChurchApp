using System.Text.Json.Serialization;

namespace Gsbc.AppPoc.Data.Outreach;

public sealed record LetterboxTally(int Placed, int Unplaced, int Skipped = 0);

public sealed record Mark(double Lon, double Lat, int Side, int Edge, string Label)
{
    public GeoPoint Point => new(Lat, Lon);
}

public sealed record StreetEdge(int U, int V, string Name, string Road, int Length, int[] Boxes, int[] Units, double[][] Line)
{
    public bool IsArterial => Road is "primary" or "secondary";

    public int TotalBoxes => Boxes[0] + Boxes[1];
}

public sealed record OutreachArea(int Id, string Name, int[] Edges, int? LastDoneWeeksAgo);

public sealed record ReadyRoute(int Id, string Name, int[] Edges);

public sealed record OutreachMap(
    string Attribution,
    double[] Church,
    double[] Bounds,
    LetterboxTally Letterboxes,
    IReadOnlyList<StreetEdge> Edges,
    IReadOnlyList<OutreachArea> Areas,
    IReadOnlyList<ReadyRoute> OutreachRoutes,
    IReadOnlyList<Mark>? Skips = null,
    IReadOnlyList<Mark>? Checks = null);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(OutreachMap))]
public sealed partial class OutreachMapJson : JsonSerializerContext;
