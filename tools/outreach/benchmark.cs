#:project ../../src/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Routing/GSBC.ChurchApp.Features.Outreach.Routing.csproj
#:property PublishAot=false

using System.Diagnostics;
using GSBC.ChurchApp.Features.Outreach.Routing;

string fixture = args.Length > 0 ? args[0] : "tests/Features/Outreach/GSBC.ChurchApp.Features.Outreach.Routing.Tests/Fixtures/albany-creek.streets";
int across = args.Length > 1 ? int.Parse(args[1]) : 3, down = args.Length > 2 ? int.Parse(args[2]) : 2;

StreetGraph tile = StreetGraphFile.Read(File.ReadAllBytes(fixture));
byte[] region = StreetGraphFile.ToBytes(Tiled(tile, across, down));
Console.WriteLine($"Region: {across}×{down} copies of {tile.Region.Name}, {region.Length / 1024} KB on disk");

Measure("Load the region graph", 10, () => StreetGraphFile.Read(region));
StreetGraph graph = StreetGraphFile.Read(region);
Console.WriteLine($"  {graph.StreetCount} streets, {graph.NodeCount} nodes, {graph.LetterboxCount} letterboxes");
WalkNetwork network = new(graph);
Measure("Load it and build its street index", 10, () => StreetGraphFile.Read(region).Index);
_ = graph.Index;
Measure("Work out what's open", 10, () => new Openness(network, [], new DateOnly(2026, 11, 14), RouteSettings.Defaults));
Openness open = new(network, [], new DateOnly(2026, 11, 14), RouteSettings.Defaults);
WalkPlanner planner = new(open);
ReachFinder reach = new(planner);
GeoPoint church = tile.Region.Origin;
int start = planner.StartNear(church)!.Value;
foreach (double minutes in new[] { 30.0, 60, 120 })
{
    WalkBudget budget = new(minutes);
    Measure($"Plan a {minutes} min walk", 20, () => planner.Plan(start, budget));
    WalkPlan plan = planner.Plan(start, budget);
    Measure($"  and its reach", 20, () => reach.Assess(start, budget, plan));
}
Random random = new(7);
List<int> starts = Enumerable.Range(0, 50).Select(_ => planner.StartNear(new GeoPoint(church.Lat + (random.NextDouble() - 0.5) * 0.08, church.Lon + (random.NextDouble() - 0.5) * 0.12))).OfType<int>().ToList();
Stopwatch all = Stopwatch.StartNew();
double worst = 0;
foreach (int s in starts)
{
    Stopwatch one = Stopwatch.StartNew();
    WalkPlan plan = planner.Plan(s, new WalkBudget(60));
    reach.Assess(s, new WalkBudget(60), plan);
    worst = Math.Max(worst, one.Elapsed.TotalMilliseconds);
}
Console.WriteLine($"{"An hour's walk and its reach from 50 places",-48} {all.Elapsed.TotalMilliseconds / starts.Count,8:0.0} ms each, worst {worst:0.0} ms");

static void Measure(string what, int times, Func<object> work)
{
    work();
    Stopwatch watch = Stopwatch.StartNew();
    for (int i = 0; i < times; i++)
        work();
    Console.WriteLine($"{what,-48} {watch.Elapsed.TotalMilliseconds / times,8:0.0} ms");
}

static StreetGraph Tiled(StreetGraph tile, int across, int down)
{
    GeoBounds box = tile.Region.Bounds;
    LocalProjection projection = tile.Projection;
    Flat low = projection.ToFlat(new GeoPoint(box.South, box.West)), high = projection.ToFlat(new GeoPoint(box.North, box.East));
    Flat size = high - low;
    StreetRegion region = tile.Region with
    {
        Bounds = GeoBounds.Around([new GeoPoint(box.South, box.West), projection.ToGeo(low + new Flat(size.X * across, size.Y * down))]),
    };
    region = region with { Core = region.Bounds };
    StreetGraphBuilder builder = new(region);
    long osmStep = tile.NodeCount + 1;
    for (int i = 0; i < across; i++)
    {
        for (int j = 0; j < down; j++)
        {
            Flat shift = new(size.X * i, size.Y * j);
            long copy = i * down + j;
            int first = -1;
            for (int n = 0; n < tile.NodeCount; n++)
            {
                int node = builder.AddNode(projection.ToGeo(tile.NodeFlat(n) + shift), tile.NodeOsmId(n) + copy * osmStep);
                first = first < 0 ? node : first;
            }
            for (int s = 0; s < tile.StreetCount; s++)
            {
                StreetInfo info = tile.Street(s);
                ReadOnlySpan<Flat> line = tile.Shape(s);
                List<GeoPoint> inner = [];
                for (int k = 1; k < line.Length - 1; k++)
                    inner.Add(projection.ToGeo(line[k] + shift));
                int street = builder.AddStreet(first + info.U, first + info.V, inner, info.Name, info.Kind, info.Way + copy * 1_000_000, info.FullName);
                for (int side = 0; side < 2; side++)
                {
                    LetterboxRange range = tile.Letterboxes(s, (Side)side);
                    for (int b = range.Start; b < range.End; b++)
                        builder.AddLetterbox(street, (Side)side, tile.LetterboxAlong(b), tile.LetterboxKindOf(b), tile.LetterboxLabel(b), tile.LetterboxAddressKey(b));
                }
            }
        }
    }
    return builder.Build();
}
