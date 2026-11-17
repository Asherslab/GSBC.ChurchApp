using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.National;

internal sealed record GnafFile(string Name, Uri Url);

internal interface IGnafSource
{
    Task<GnafFile> LatestAsync(CancellationToken cancellationToken);

    Task DownloadAsync(GnafFile file, string path, CancellationToken cancellationToken);
}

internal sealed class GnafSource(HttpClient http, IOptions<MapSourcesOptions> options) : IGnafSource
{
    private const string Datum = "GDA2020";
    private static readonly TimeSpan Download = TimeSpan.FromMinutes(60);

    public async Task<GnafFile> LatestAsync(CancellationToken cancellationToken)
    {
        using JsonDocument package = await Patience.TryAsync(async limit =>
        {
            await using Stream stream = await http.GetStreamAsync(options.Value.NationalAddresses, limit);
            return await JsonDocument.ParseAsync(stream, cancellationToken: limit);
        }, TimeSpan.FromMinutes(1), 4, TimeSpan.FromSeconds(10), cancellationToken);
        return package.RootElement.GetProperty("result").GetProperty("resources").EnumerateArray()
            .Where(resource => Text(resource, "format").Equals("zip", StringComparison.OrdinalIgnoreCase)
                               && Text(resource, "name").Contains(Datum, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(resource => Text(resource, "last_modified"), StringComparer.Ordinal)
            .Select(resource => new GnafFile(Text(resource, "name"), new Uri(Text(resource, "url"))))
            .FirstOrDefault() ?? throw new InvalidOperationException("data.gov.au lists no G-NAF download");
    }

    public Task DownloadAsync(GnafFile file, string path, CancellationToken cancellationToken) =>
        Patience.TryAsync(async limit =>
        {
            await using Stream download = await http.GetStreamAsync(file.Url, limit);
            await using FileStream saved = File.Create(path);
            await download.CopyToAsync(saved, limit);
            return true;
        }, Download, 3, TimeSpan.FromMinutes(1), cancellationToken);

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
}
