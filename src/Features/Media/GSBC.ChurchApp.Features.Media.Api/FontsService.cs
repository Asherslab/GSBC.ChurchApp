using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Media.Api;

internal sealed class FontsService(GoogleFonts google, MediaLibrary library) : IFontsService
{
    private const int MostResults = 30;

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

        ClaimsPrincipal user = context.ServerCallContext?.GetHttpContext().User ?? new ClaimsPrincipal();
        CustomFont font = new() { Family = family.Family };
        foreach (GoogleFontFile file in files)
        {
            string fileName = $"{family.Family} {file.Weight}.woff2";
            MediaRef stored = await library.StoreAsync(user, file.Bytes, "font/woff2", fileName, context.CancellationToken);
            font.Files.Add(new FontFile { Media = stored, Weight = file.Weight });
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
