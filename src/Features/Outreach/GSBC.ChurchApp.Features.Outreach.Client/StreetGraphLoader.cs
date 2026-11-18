using System.Collections.Concurrent;
using GSBC.ChurchApp.Features.Outreach.Maps.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Outreach.Client;

[AdminOnly]
public sealed class StreetGraphLoader(HttpClient http, TimeProvider time)
{
    private readonly ConcurrentDictionary<(string Region, long Version), Lazy<Task<StreetGraph>>> _graphs = new();

    public async Task<StreetGraph> LoadAsync(OutreachRegion region)
    {
        (string, long) key = (region.Id, region.Version);
        try
        {
            return await _graphs.GetOrAdd(key, _ => new Lazy<Task<StreetGraph>>(() => FetchAsync(region.Streets.Url))).Value;
        }
        catch (HttpRequestException)
        {
            _graphs.TryRemove(key, out _);
            throw;
        }
    }

    private async Task<StreetGraph> FetchAsync(string url)
    {
        byte[] bytes = await http.GetByteArrayAsync(url);
        return await new FrameBudget(time).RunAsync(StreetGraphFile.ReadInSteps(bytes));
    }
}
