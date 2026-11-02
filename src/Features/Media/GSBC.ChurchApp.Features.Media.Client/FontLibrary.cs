using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class FontLibrary(ChannelBase channel, MediaClient media, IAccess access) : IFontLibrary
{
    private readonly IFontsService _fonts = channel.CreateGrpcService<IFontsService>();

    public bool MayChooseFromGoogle => access.Can(MediaPermissions.Upload);

    public bool MayUploadFiles => access.Can(MediaPermissions.Upload) && access.Can(MediaPermissions.UploadFonts);

    public async Task<IReadOnlyList<FontFamilyChoice>> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        (await _fonts.SearchAsync(new FontSearch { Query = query })).Families;

    public async Task<CustomFont> ImportAsync(string family, CancellationToken cancellationToken = default) =>
        await _fonts.ImportAsync(new ImportFontRequest { Family = family });

    public async Task<CustomFont> UploadAsync(string fileName, long bytes, Stream content, CancellationToken cancellationToken = default)
    {
        string contentType = MediaTypes.FontTypeOf(fileName)
            ?? throw new RpcException(new Status(StatusCode.InvalidArgument, $"{fileName} isn't a font file. Use .woff2, .woff, .ttf or .otf."));
        MediaRef uploaded = await media.UploadAsync(new BeginUploadRequest { FileName = fileName, ContentType = contentType, Bytes = bytes }, content, cancellationToken);
        return new CustomFont
        {
            Family = Path.GetFileNameWithoutExtension(fileName),
            Files = [new FontFile { Media = uploaded, Weight = FontFile.RegularToBold }],
        };
    }
}
