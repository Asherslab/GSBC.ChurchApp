using System.Globalization;
using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;

internal sealed class OverpassStreets(HttpClient http)
{
    private const double ChunkDegrees = 0.1;
    private const int Attempts = 5;

    public async Task<OsmData> FetchAsync(IReadOnlyList<Uri> servers, GeoBounds bounds, CancellationToken cancellationToken)
    {
        OsmData all = new();
        for (double south = bounds.South; south < bounds.North; south += ChunkDegrees)
            for (double west = bounds.West; west < bounds.East; west += ChunkDegrees)
            {
                GeoBounds chunk = new(south, west, Math.Min(south + ChunkDegrees, bounds.North), Math.Min(west + ChunkDegrees, bounds.East));
                all.Add(await ChunkAsync(servers, chunk, cancellationToken));
            }
        return all;
    }

    private static string QueryFor(GeoBounds chunk) => string.Create(CultureInfo.InvariantCulture,
        $"[out:json][timeout:180];way[\"highway\"~\"^({string.Join('|', StreetNetwork.Highways)})$\"]({chunk.South},{chunk.West},{chunk.North},{chunk.East});out body;>;out skel qt;");

    private async Task<OsmData> ChunkAsync(IReadOnlyList<Uri> servers, GeoBounds chunk, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using FormUrlEncodedContent body = new([new KeyValuePair<string, string>("data", QueryFor(chunk))]);
                using HttpResponseMessage response = await http.PostAsync(servers[(attempt - 1) % servers.Count], body, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                return Read(document.RootElement);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException
                                              && attempt < Attempts * servers.Count && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt % servers.Count == 0 ? 20 * attempt / servers.Count : 1), cancellationToken);
            }
        }
    }

    private static OsmData Read(JsonElement root)
    {
        OsmData data = new();
        foreach (JsonElement element in root.GetProperty("elements").EnumerateArray())
        {
            long id = element.GetProperty("id").GetInt64();
            switch (element.GetProperty("type").GetString())
            {
                case "node":
                    data.Nodes[id] = new GeoPoint(element.GetProperty("lat").GetDouble(), element.GetProperty("lon").GetDouble());
                    break;
                case "way":
                    Dictionary<string, string> tags = [];
                    if (element.TryGetProperty("tags", out JsonElement tagList))
                        foreach (JsonProperty tag in tagList.EnumerateObject())
                            tags[tag.Name] = tag.Value.GetString() ?? "";
                    data.Ways[id] = new OsmWay(id, element.GetProperty("nodes").EnumerateArray().Select(node => node.GetInt64()).ToArray(), tags);
                    break;
            }
        }
        return data;
    }
}
