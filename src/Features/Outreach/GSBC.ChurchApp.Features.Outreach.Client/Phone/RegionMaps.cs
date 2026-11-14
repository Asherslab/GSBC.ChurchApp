using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Client.Files;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record RegionOnPhone(OutreachRegion Region, KeptFile? Map, KeptFile? Streets, bool Chosen)
{
    public bool Ready => Map?.State == KeptFileState.Done && Streets?.State == KeptFileState.Done;

    public bool Downloading => Map?.State == KeptFileState.Downloading || Streets?.State == KeptFileState.Downloading;

    public bool Failed => Map?.State == KeptFileState.Failed || Streets?.State == KeptFileState.Failed;

    public bool Kept => Map is not null || Streets is not null;

    public long Bytes => Region.Map.Bytes + Region.Streets.Bytes;

    public double Fraction => Map is null || Streets is null ? 0
        : (double)(Map.Fraction * Region.Map.Bytes + Streets.Fraction * Region.Streets.Bytes) / Math.Max(1, Bytes);
}

public sealed class RegionMaps(IKeptFiles kept, ILocalPreferences preferences)
{
    private const string ChosenKey = "outreach.regions.chosen";
    private readonly Dictionary<string, (long Version, StreetGraph Graph)> _graphs = [];
    private readonly SemaphoreSlim _loading = new(1, 1);

    public bool KeepsFiles => kept.IsAvailable;

    public event Action? Changed
    {
        add => kept.Changed += value;
        remove => kept.Changed -= value;
    }

    public static OutreachRegion? RegionAt(IReadOnlyList<OutreachRegion> regions, GeoPoint point) =>
        regions.FirstOrDefault(region => Bounds(region.Core).Contains(point))
        ?? regions.FirstOrDefault(region => Bounds(region.Bounds).Contains(point));

    public static OutreachRegion? Nearest(IReadOnlyList<OutreachRegion> regions, GeoPoint point) =>
        RegionAt(regions, point) ?? regions.MinBy(region => Distance(Bounds(region.Core).Centre, point));

    public static GeoBounds Bounds(MapBounds bounds) => new(bounds.South, bounds.West, bounds.North, bounds.East);

    public MapSource SourceFor(RegionOnPhone region) =>
        region.Map?.State == KeptFileState.Done || (kept.IsAvailable && region.Map is not null)
            ? MapSource.KeptFile(MapId(region.Region), region.Region.Attribution)
            : MapSource.Url(region.Region.Map.Url, region.Region.Attribution);

    public async Task<IReadOnlyList<RegionOnPhone>> ListAsync(IReadOnlyList<OutreachRegion> regions)
    {
        Dictionary<string, KeptFile> files = kept.IsAvailable ? (await kept.ListAsync()).ToDictionary(file => file.Id) : [];
        HashSet<string> chosen = await ChosenAsync();
        return [.. regions.Select(region => new RegionOnPhone(region, files.GetValueOrDefault(MapId(region)), files.GetValueOrDefault(StreetsId(region)), chosen.Contains(region.Id)))];
    }

    public async Task<RegionOnPhone> OnPhoneAsync(OutreachRegion region) => (await ListAsync([region]))[0];

    public async Task KeepAsync(OutreachRegion region, bool chosen)
    {
        if (chosen)
            await ChooseAsync(region.Id, true);
        if (!kept.IsAvailable)
            return;
        await kept.KeepAsync(MapId(region), region.Map.Url, $"{region.Name} map");
        await kept.KeepAsync(StreetsId(region), region.Streets.Url, $"{region.Name} streets");
    }

    public async Task RemoveAsync(OutreachRegion region)
    {
        await ChooseAsync(region.Id, false);
        _graphs.Remove(region.Id);
        if (!kept.IsAvailable)
            return;
        await kept.RemoveAsync(MapId(region));
        await kept.RemoveAsync(StreetsId(region));
    }

    public async Task<StreetGraph?> GraphAsync(OutreachRegion region)
    {
        await _loading.WaitAsync();
        try
        {
            if (_graphs.TryGetValue(region.Id, out var held) && held.Version == region.Version)
                return held.Graph;
            byte[]? bytes = await StreetsBytesAsync(region);
            if (bytes is null)
                return null;
            StreetGraph graph = StreetGraphFile.Read(bytes);
            _graphs[region.Id] = (region.Version, graph);
            return graph;
        }
        finally
        {
            _loading.Release();
        }
    }

    private async Task<byte[]?> StreetsBytesAsync(OutreachRegion region)
    {
        if (kept.IsAvailable)
        {
            KeptFile? file = (await kept.ListAsync()).FirstOrDefault(entry => entry.Id == StreetsId(region));
            if (file is null || file.Url != region.Streets.Url)
                await KeepAsync(region, false);
            return file?.State == KeptFileState.Done ? await kept.ReadAsync(StreetsId(region)) : null;
        }
        return null;
    }

    private async Task<HashSet<string>> ChosenAsync() =>
        (await preferences.GetAsync(ChosenKey) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

    private async Task ChooseAsync(string region, bool chosen)
    {
        HashSet<string> all = await ChosenAsync();
        if (chosen ? all.Add(region) : all.Remove(region))
            await preferences.SetAsync(ChosenKey, string.Join(',', all));
    }

    private static string MapId(OutreachRegion region) => $"outreach/{region.Id}/map";

    private static string StreetsId(OutreachRegion region) => $"outreach/{region.Id}/streets";

    private static double Distance(GeoPoint a, GeoPoint b) => new LocalProjection(a).Distance(a, b);
}
