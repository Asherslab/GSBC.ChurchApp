using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class FontsService(GoogleFonts google, IMediaSystemFolders folders) : IFontsService
{
    private const int MostResults = 30;
    private const string Woff2 = "font/woff2";

    public static readonly MediaSystemArea Area = new("fonts", "Fonts from Google",
        "Fonts fetched from Google Fonts for the themes staff make. Each is kept once, however many themes use it, and a theme using it keeps it. One no theme uses can be deleted, and is fetched again if it's chosen.",
        MediaPermissions.View, StaffDeleteUnused: true);

    public async ValueTask<FontFamilies> SearchAsync(FontSearch request, CallContext context = default)
    {
        IReadOnlyList<GoogleFamily> families = await CatalogueAsync(context.CancellationToken);
        string query = request.Query.Trim();
        return new FontFamilies
        {
            Families = families
                .Where(family => query.Length == 0 || family.Family.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(family => query.Length > 0 && !family.Family.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Take(MostResults)
                .Select(family => new FontFamilyChoice { Family = family.Family, Category = family.Category })
                .ToList(),
        };
    }

    public async ValueTask<CustomFont> ImportAsync(ImportFontRequest request, CallContext context = default)
    {
        GoogleFamily family = (await CatalogueAsync(context.CancellationToken))
            .FirstOrDefault(known => string.Equals(known.Family, request.Family, StringComparison.OrdinalIgnoreCase))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Google Fonts has no font called {request.Family}."));

        IReadOnlyList<GoogleFontFile> files;
        try
        {
            files = await google.DownloadAsync(family, context.CancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, $"Couldn't fetch {family.Family} from Google Fonts. Try again in a moment."));
        }

        CustomFont font = new() { Family = family.Family };
        foreach (GoogleFontFile file in files)
        {
            using MemoryStream content = new(file.Bytes);
            if (!(await MediaInspector.InspectAsync(content, Woff2, context.CancellationToken)).MatchesType)
                throw new RpcException(new Status(StatusCode.Unavailable, $"Google Fonts sent something for {family.Family} that isn't a font. Try again in a moment."));
            MediaTypes.TryGetExtension(Woff2, out string extension);
            MediaRef kept = await folders.KeepAsync(Area, new KeptFile(file.Bytes, Woff2, extension, $"{family.Family} {file.Weight}.woff2"), context.CancellationToken);
            font.Files.Add(new FontFile { Media = kept, Weight = file.Weight });
        }
        return font;
    }

    private async Task<IReadOnlyList<GoogleFamily>> CatalogueAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await google.FamiliesAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new RpcException(new Status(StatusCode.Unavailable, "Google Fonts can't be reached right now. Try again in a moment."));
        }
    }
}
