using System.Globalization;
using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;

internal sealed record ArcGisFeature(IReadOnlyDictionary<string, string> Attributes, GeoPoint? Point, IReadOnlyList<GeoPoint[]> Rings);

internal sealed class ArcGisLayer(HttpClient http)
{
    private const int Attempts = 4;

    public async Task<List<ArcGisFeature>> QueryAsync(Uri layer, GeoBounds bounds, string fields, string where, string orderBy, int pageSize, bool geometry,
        CancellationToken cancellationToken)
    {
        List<ArcGisFeature> features = [];
        for (int offset = 0; ; )
        {
            Dictionary<string, string> query = new()
            {
                ["geometry"] = Envelope(bounds),
                ["geometryType"] = "esriGeometryEnvelope",
                ["inSR"] = "4326",
                ["spatialRel"] = "esriSpatialRelIntersects",
                ["where"] = where,
                ["outFields"] = fields,
                ["returnGeometry"] = geometry ? "true" : "false",
                ["geometryPrecision"] = "6",
                ["outSR"] = "4326",
                ["orderByFields"] = orderBy,
                ["resultOffset"] = offset.ToString(CultureInfo.InvariantCulture),
                ["resultRecordCount"] = pageSize.ToString(CultureInfo.InvariantCulture),
                ["f"] = "json",
            };
            using JsonDocument page = await PageAsync(layer, query, cancellationToken);
            JsonElement root = page.RootElement;
            if (root.TryGetProperty("error", out JsonElement error))
                throw new HttpRequestException($"{layer.Host} answered: {error}");
            int count = 0;
            foreach (JsonElement feature in root.GetProperty("features").EnumerateArray())
            {
                features.Add(Read(feature));
                count++;
            }
            bool more = root.TryGetProperty("exceededTransferLimit", out JsonElement exceeded) && exceeded.GetBoolean();
            if (!more || count == 0)
                return features;
            offset += count;
        }
    }

    public async Task<int> CountAsync(Uri layer, GeoBounds bounds, string where, CancellationToken cancellationToken)
    {
        Dictionary<string, string> query = new()
        {
            ["geometry"] = Envelope(bounds),
            ["geometryType"] = "esriGeometryEnvelope",
            ["inSR"] = "4326",
            ["spatialRel"] = "esriSpatialRelIntersects",
            ["where"] = where,
            ["returnCountOnly"] = "true",
            ["f"] = "json",
        };
        using JsonDocument answer = await PageAsync(layer, query, cancellationToken);
        JsonElement root = answer.RootElement;
        if (root.TryGetProperty("error", out JsonElement error))
            throw new HttpRequestException($"{layer.Host} answered: {error}");
        return root.GetProperty("count").GetInt32();
    }

    private static string Envelope(GeoBounds bounds) => string.Create(CultureInfo.InvariantCulture, $"{bounds.West},{bounds.South},{bounds.East},{bounds.North}");

    private async Task<JsonDocument> PageAsync(Uri layer, Dictionary<string, string> query, CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                using FormUrlEncodedContent body = new(query);
                using HttpResponseMessage response = await http.PostAsync(layer, body, cancellationToken);
                response.EnsureSuccessStatusCode();
                await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException
                                              && attempt < Attempts && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10 * attempt), cancellationToken);
            }
        }
    }

    private static ArcGisFeature Read(JsonElement feature)
    {
        Dictionary<string, string> attributes = [];
        foreach (JsonProperty property in feature.GetProperty("attributes").EnumerateObject())
            attributes[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? "",
                JsonValueKind.Number => property.Value.GetRawText(),
                _ => "",
            };
        GeoPoint? point = null;
        List<GeoPoint[]> rings = [];
        if (feature.TryGetProperty("geometry", out JsonElement geometry) && geometry.ValueKind == JsonValueKind.Object)
        {
            if (geometry.TryGetProperty("x", out JsonElement x) && x.ValueKind == JsonValueKind.Number)
                point = new GeoPoint(geometry.GetProperty("y").GetDouble(), x.GetDouble());
            if (geometry.TryGetProperty("rings", out JsonElement ringList))
                foreach (JsonElement ring in ringList.EnumerateArray())
                    rings.Add(ring.EnumerateArray().Select(pair => new GeoPoint(pair[1].GetDouble(), pair[0].GetDouble())).ToArray());
        }
        return new ArcGisFeature(attributes, point, rings);
    }
}
