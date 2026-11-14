#:project ../../src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Routing/GSBC.ChurchApp.Features.Outreach.Routing.csproj
#:property PublishAot=false

using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Routing;

string source = args.Length > 0 ? args[0] : "spikes/app-poc/App/wwwroot/data/outreach.json";
string target = args.Length > 1 ? args[1] : "tests/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Routing.Tests/Fixtures/albany-creek.streets";

using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(source));
JsonElement root = document.RootElement;
double[] bounds = root.GetProperty("bounds").EnumerateArray().Select(e => e.GetDouble()).ToArray();
double[] church = root.GetProperty("church").EnumerateArray().Select(e => e.GetDouble()).ToArray();
GeoBounds box = new(bounds[0], bounds[1], bounds[2], bounds[3]);
StreetRegion region = new(
    "albany-creek-poc",
    "Albany Creek (POC data)",
    20261027,
    new DateTimeOffset(2026, 10, 27, 0, 0, 0, TimeSpan.Zero),
    box,
    box,
    new GeoPoint(church[0], church[1]),
    root.GetProperty("attribution").GetString()!);

StreetGraphBuilder builder = new(region);
JsonElement[] edges = root.GetProperty("edges").EnumerateArray().ToArray();
Dictionary<int, int> nodes = [];
int Node(int pocId, JsonElement point)
{
    if (!nodes.TryGetValue(pocId, out int node))
        nodes[pocId] = node = builder.AddNode(new GeoPoint(point[1].GetDouble(), point[0].GetDouble()), pocId + 1);
    return node;
}

List<int> streets = [];
for (int i = 0; i < edges.Length; i++)
{
    JsonElement edge = edges[i];
    JsonElement[] line = edge.GetProperty("line").EnumerateArray().ToArray();
    int u = Node(edge.GetProperty("u").GetInt32(), line[0]);
    int v = Node(edge.GetProperty("v").GetInt32(), line[^1]);
    GeoPoint[] inner = line[1..^1].Select(p => new GeoPoint(p[1].GetDouble(), p[0].GetDouble())).ToArray();
    streets.Add(builder.AddStreet(u, v, inner, edge.GetProperty("name").GetString()!, RoadKinds.FromOsm(edge.GetProperty("road").GetString()!), i + 1));
}

StreetGraph shapes = builder.Build();
Dictionary<(int Edge, int Side), List<(double Along, LetterboxKind Kind, string? Label)>> boxes = [];
for (int i = 0; i < edges.Length; i++)
{
    JsonElement edge = edges[i];
    for (int side = 0; side < 2; side++)
    {
        int units = edge.GetProperty("units")[side].GetInt32();
        boxes[(i, side)] = edge.GetProperty("at")[side].EnumerateArray()
            .Select((at, k) => (at.GetInt32() / 1000.0, k < units ? LetterboxKind.Unit : LetterboxKind.House, (string?)null))
            .ToList();
    }
}
foreach (JsonElement mark in root.GetProperty("checks").EnumerateArray())
{
    (int edge, int side) = (mark.GetProperty("edge").GetInt32(), mark.GetProperty("side").GetInt32());
    double along = Along(edge, mark);
    var list = boxes[(edge, side)];
    if (list.Count == 0)
        continue;
    int nearest = Enumerable.Range(0, list.Count).MinBy(k => Math.Abs(list[k].Along - along));
    list[nearest] = (list[nearest].Along, LetterboxKind.Check, mark.GetProperty("label").GetString());
}
foreach (JsonElement mark in root.GetProperty("skips").EnumerateArray())
{
    (int edge, int side) = (mark.GetProperty("edge").GetInt32(), mark.GetProperty("side").GetInt32());
    boxes[(edge, side)].Add((Along(edge, mark), LetterboxKind.Skip, mark.GetProperty("label").GetString()));
}
uint key = 1;
foreach (((int edge, int side), var list) in boxes.OrderBy(b => b.Key))
{
    foreach ((double along, LetterboxKind kind, string? label) in list)
        builder.AddLetterbox(streets[edge], (Side)side, along, kind, label, key++);
}

StreetGraph graph = builder.Build();
Directory.CreateDirectory(Path.GetDirectoryName(target)!);
using (FileStream file = File.Create(target))
    StreetGraphFile.Write(graph, file);
Console.WriteLine($"{graph.NodeCount} nodes, {graph.StreetCount} streets, {graph.LetterboxCount} letterboxes, {new FileInfo(target).Length / 1024} KB → {target}");

double Along(int edge, JsonElement mark) =>
    shapes.Project(streets[edge], shapes.Projection.ToFlat(new GeoPoint(mark.GetProperty("lat").GetDouble(), mark.GetProperty("lon").GetDouble()))).Along;
