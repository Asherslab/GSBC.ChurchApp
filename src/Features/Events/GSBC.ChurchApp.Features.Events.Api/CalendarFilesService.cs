using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class CalendarFilesService(CalendarFiles files, CalendarLinkSecrets links) : ICalendarFilesService
{
    public async ValueTask<CalendarFile> ReadAsync(CalendarFileRequest request, CallContext context = default) =>
        await files.ReadAsync(request, context.CancellationToken);

    public async ValueTask<CalendarLink> LinkAsync(Done request, CallContext context = default) =>
        new() { Secret = await links.OfAsync(PhoneHolder(context), context.CancellationToken) };

    public async ValueTask<CalendarLink> ResetLinkAsync(Done request, CallContext context = default) =>
        new() { Secret = await links.ResetAsync(PhoneHolder(context), context.CancellationToken) };

    public async ValueTask<CalendarLink> PersonLinkAsync(Done request, CallContext context = default) =>
        new() { Secret = await links.OfAsync(PersonHolder(context), context.CancellationToken) };

    public async ValueTask<CalendarLink> ResetPersonLinkAsync(Done request, CallContext context = default) =>
        new() { Secret = await links.ResetAsync(PersonHolder(context), context.CancellationToken) };

    private static LinkHolder PhoneHolder(CallContext context) => new(CallerId(context), CallingInstall.Of(context));

    private static LinkHolder PersonHolder(CallContext context) => new(CallerId(context), null);

    private static Guid? CallerId(CallContext context) =>
        context.ServerCallContext?.GetHttpContext().User is { } user ? Caller.UserId(user) : null;
}
