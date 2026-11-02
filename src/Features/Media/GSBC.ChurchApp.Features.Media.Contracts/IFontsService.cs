using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Media.Contracts;

[Service("gsbc.media.Fonts")]
public interface IFontsService
{
    [Requires(MediaPermissions.UploadNode)]
    ValueTask<FontFamilies> SearchAsync(FontSearch request, CallContext context = default);

    [Requires(MediaPermissions.UploadNode)]
    ValueTask<CustomFont> ImportAsync(ImportFontRequest request, CallContext context = default);
}
