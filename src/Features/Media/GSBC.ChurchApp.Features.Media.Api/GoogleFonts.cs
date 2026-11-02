using System.Text.Json;
using System.Text.RegularExpressions;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed record GoogleFamily(string Family, string Category, IReadOnlySet<string> Styles);

internal sealed record GoogleFontFile(string Weight, byte[] Bytes);

internal sealed partial class GoogleFonts(HttpClient http, TimeProvider time)
{
    public const string BrowserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36";

    private static readonly Uri Catalogue = new("https://fonts.google.com/metadata/fonts");
    private static readonly TimeSpan CatalogueKeptFor = TimeSpan.FromDays(1);
    private static readonly SemaphoreSlim Loading = new(1, 1);
    private static (DateTimeOffset At, IReadOnlyList<GoogleFamily> Families)? _cached;

    public async Task<IReadOnlyList<GoogleFamily>> FamiliesAsync(CancellationToken cancellationToken)
    {
        if (_cached is { } fresh && time.GetUtcNow() - fresh.At < CatalogueKeptFor)
            return fresh.Families;

        await Loading.WaitAsync(cancellationToken);
        try
        {
            if (_cached is { } loaded && time.GetUtcNow() - loaded.At < CatalogueKeptFor)
                return loaded.Families;
            string text = await http.GetStringAsync(Catalogue, cancellationToken);
            using JsonDocument document = JsonDocument.Parse(text[text.IndexOf('{')..]);
            List<GoogleFamily> families = document.RootElement.GetProperty("familyMetadataList").EnumerateArray()
                .Select(family => new GoogleFamily(
                    family.GetProperty("family").GetString() ?? "",
                    family.GetProperty("category").GetString() ?? "",
                    family.GetProperty("fonts").EnumerateObject().Select(style => style.Name).ToHashSet()))
                .Where(family => family.Family.Length > 0)
                .ToList();
            _cached = (time.GetUtcNow(), families);
            return families;
        }
        finally
        {
            Loading.Release();
        }
    }

    public async Task<IReadOnlyList<GoogleFontFile>> DownloadAsync(GoogleFamily family, CancellationToken cancellationToken)
    {
        string[] weights = new[] { FontFile.Regular, FontFile.Bold }.Where(family.Styles.Contains).ToArray();
        if (weights.Length == 0)
            weights = [family.Styles.Where(style => style.All(char.IsAsciiDigit)).OrderBy(style => Math.Abs(int.Parse(style) - 400)).First()];

        Uri css = new($"https://fonts.googleapis.com/css2?family={Uri.EscapeDataString(family.Family)}:wght@{string.Join(';', weights)}&display=swap");
        string sheet = await http.GetStringAsync(css, cancellationToken);

        Dictionary<string, string> latinByWeight = [];
        foreach (Match face in LatinFace().Matches(sheet))
            latinByWeight.TryAdd(face.Groups["weight"].Value, face.Groups["url"].Value);
        if (latinByWeight.Count == 0)
            throw new InvalidOperationException($"Google Fonts sent no Latin files for {family.Family}.");

        List<GoogleFontFile> files = [];
        foreach (IGrouping<string, KeyValuePair<string, string>> sameFile in latinByWeight.GroupBy(pair => pair.Value))
        {
            string weight = sameFile.Count() > 1 ? FontFile.RegularToBold : sameFile.First().Key;
            files.Add(new GoogleFontFile(weight, await http.GetByteArrayAsync(sameFile.Key, cancellationToken)));
        }
        return files;
    }

    [GeneratedRegex(@"/\* latin \*/\s*@font-face\s*\{[^}]*?font-weight:\s*(?<weight>\d+);[^}]*?src:\s*url\((?<url>https://fonts\.gstatic\.com/[^)]+\.woff2)\)")]
    private static partial Regex LatinFace();
}
