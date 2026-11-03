using Grpc.Core;
using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class NotificationsService(NotificationsDbContext db, NotificationKindCatalog kinds, TimeProvider time) : INotificationsService
{
    private static readonly HashSet<string> Platforms = ["ios"];
    private static readonly HashSet<string> Environments = [Push.ApnsTransport.SandboxEnvironment, "production"];

    public async ValueTask<Done> MarkReadAsync(MarkReadRequest request, CallContext context = default)
    {
        Guid userId = SignedInUser(context);
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<Guid> ids = request.Ids.Select(id => Guid.TryParse(id, out Guid parsed) ? parsed : Guid.Empty).ToList();
        List<InboxEntry> entries = await db.Inbox.Include(entry => entry.Notification)
            .Where(entry => entry.UserId == userId && entry.ReadAt == null && (request.All || ids.Contains(entry.Id)))
            .ToListAsync(context.CancellationToken);
        if (entries.Count == 0)
            return new Done();

        List<string> recordIds = entries.Select(entry => entry.Id.ToString()).ToList();
        Dictionary<string, long> versions = await db.FeedChanges.AsNoTracking()
            .Where(change => change.Feed == NotificationsFeeds.Inbox && recordIds.Contains(change.RecordId))
            .GroupBy(change => change.RecordId)
            .Select(group => new { group.Key, Version = group.Max(change => change.Version) })
            .ToDictionaryAsync(group => group.Key, group => group.Version, context.CancellationToken);

        foreach (InboxEntry entry in entries)
        {
            entry.ReadAt = now;
            db.FeedChanges.Add(InboxPayloads.InboxRow(entry, entry.Notification!, versions.GetValueOrDefault(entry.Id.ToString()) + 1));
        }
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SetPreferenceAsync(PreferenceItem request, CallContext context = default)
    {
        Guid userId = SignedInUser(context);
        Preference preference = await SaveAsync(request, userId, installId: null, context.CancellationToken);
        long version = await db.FeedChanges.Where(change => change.Feed == NotificationsFeeds.Preferences && change.RecordId == preference.Id.ToString())
            .MaxAsync(change => (long?)change.Version, context.CancellationToken) ?? 0;
        db.FeedChanges.Add(new FeedChangeRecord
        {
            Feed = NotificationsFeeds.Preferences,
            RecordId = preference.Id.ToString(),
            ScopeId = userId.ToString(),
            Version = version + 1,
            Payload = InboxPayloads.Encode(new PreferenceItem { Kind = preference.Kind, ScopeId = preference.ScopeId, Push = preference.Push }),
        });
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> SetPhonePreferenceAsync(PreferenceItem request, CallContext context = default)
    {
        await SaveAsync(request, userId: null, CallingInstall.Of(context), context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    public async ValueTask<Done> RegisterDeviceAsync(RegisterDeviceRequest request, CallContext context = default)
    {
        if (!Platforms.Contains(request.Platform) || !Environments.Contains(request.Environment) || request.Token is not { Length: > 0 and <= 200 } || request.AppId.Length == 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "That isn't a push token this server can use."));

        Guid installId = CallingInstall.Of(context);
        Guid? userId = Caller.UserId(context.ServerCallContext!.GetHttpContext().User);
        DateTime now = time.GetUtcNow().UtcDateTime;
        await db.Devices.Where(device => device.Token == request.Token && device.InstallId != installId).ExecuteDeleteAsync(context.CancellationToken);
        Device? device = await db.Devices.SingleOrDefaultAsync(candidate => candidate.InstallId == installId, context.CancellationToken);
        if (device is null)
        {
            device = new Device
            {
                Id = Guid.CreateVersion7(), InstallId = installId, Platform = request.Platform, AppId = request.AppId,
                Environment = request.Environment, Token = request.Token,
            };
            db.Devices.Add(device);
        }

        device.UserId = userId;
        device.Token = request.Token;
        device.AppId = request.AppId;
        device.Environment = request.Environment;
        device.RegisteredAt = now;
        if (userId is not null)
            await db.Preferences.Where(preference => preference.InstallId == installId).ExecuteDeleteAsync(context.CancellationToken);
        await db.SaveChangesAsync(context.CancellationToken);
        return new Done();
    }

    private async Task<Preference> SaveAsync(PreferenceItem request, Guid? userId, Guid? installId, CancellationToken cancellationToken)
    {
        if (!kinds.ByKey.TryGetValue(request.Kind, out var kind))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "No such kind of notification."));
        if (kind.Mandatory)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "This one is always on: it's about your account's safety."));
        string? scopeId = string.IsNullOrEmpty(request.ScopeId) ? null : request.ScopeId;

        Preference? preference = await db.Preferences.SingleOrDefaultAsync(candidate =>
            candidate.UserId == userId && candidate.InstallId == installId && candidate.Kind == request.Kind && candidate.ScopeId == scopeId, cancellationToken);
        if (preference is null)
        {
            preference = new Preference { Id = Guid.CreateVersion7(), UserId = userId, InstallId = installId, Kind = request.Kind, ScopeId = scopeId };
            db.Preferences.Add(preference);
        }
        preference.Push = request.Push;
        return preference;
    }

    private static Guid SignedInUser(CallContext context) =>
        Caller.UserId(context.ServerCallContext!.GetHttpContext().User) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in first."));
}
