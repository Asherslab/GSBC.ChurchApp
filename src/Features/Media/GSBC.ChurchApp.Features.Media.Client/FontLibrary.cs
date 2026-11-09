using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class FontLibrary(ChannelBase channel, IAccess access) : IFontLibrary
{
    private readonly IFontsService _fonts = channel.CreateGrpcService<IFontsService>();

    public bool MayChooseFromGoogle => access.Can(MediaPermissions.Upload);

    public bool MayChooseFiles => access.Can(MediaPermissions.View);

    public async Task<IReadOnlyList<FontFamilyChoice>> SearchAsync(string query, CancellationToken cancellationToken = default) =>
        (await _fonts.SearchAsync(new FontSearch { Query = query }, new CallOptions(cancellationToken: cancellationToken))).Families;

    public async Task<CustomFont> ImportAsync(string family, CancellationToken cancellationToken = default) =>
        await _fonts.ImportAsync(new ImportFontRequest { Family = family }, new CallOptions(cancellationToken: cancellationToken));
}
