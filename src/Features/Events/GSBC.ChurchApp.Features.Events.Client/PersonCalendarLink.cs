using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Events.Client;

[AdminOnly]
public sealed class PersonCalendarLink(ChannelBase channel) : IYourCalendarLink
{
    private readonly ICalendarFilesService _files = channel.CreateGrpcService<ICalendarFilesService>();
    private string? _key;

    public async Task<string?> PersonalKeyAsync() => _key ??= await PhoneCalendarLink.KeyOfAsync(() => _files.PersonLinkAsync(new Done()));

    public async Task<string?> ResetAsync() => _key = await PhoneCalendarLink.KeyOfAsync(() => _files.ResetPersonLinkAsync(new Done())) ?? _key;
}
