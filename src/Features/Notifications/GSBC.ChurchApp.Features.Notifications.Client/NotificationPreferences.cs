using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Permissions;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Notifications.Client;

public sealed record KindChoice(KindInfo Kind, bool Push);

public sealed class NotificationPreferences(
    ILocalFeeds feeds, IAccess access, IOutbox outbox, ILocalPreferences local, NativeBridge bridge, ChannelBase channel)
{
    public const string SetMethod = "/gsbc.notifications.Notifications/SetPreference";
    public static readonly IReadOnlyList<string> Feeds = [NotificationsFeeds.Kinds, NotificationsFeeds.Preferences];
    private const string OnThisPhone = "notifications.push.";
    private const int MostKinds = 200;

    private readonly INotificationsService _service = channel.CreateGrpcService<INotificationsService>();

    public async Task<IReadOnlyList<KindChoice>> LoadAsync()
    {
        List<KindInfo> kinds = (await feeds.ReadPageAsync(new FeedPageQuery(NotificationsFeeds.Kinds, Limit: MostKinds)))
            .Select(record => Decode<KindInfo>(record.Payload))
            .Where(kind => kind.PerScope is null && MayReceive(kind))
            .ToList();
        Dictionary<string, bool> chosen = access.SignedIn
            ? (await feeds.ReadPageAsync(new FeedPageQuery(NotificationsFeeds.Preferences, Limit: MostKinds * 4)))
                .Select(record => Decode<PreferenceItem>(record.Payload))
                .Where(preference => preference.ScopeId is null)
                .ToDictionary(preference => preference.Kind, preference => preference.Push)
            : await OnPhoneAsync(kinds);

        return kinds.Select(kind => new KindChoice(kind, kind.Mandatory || chosen.GetValueOrDefault(kind.Key, kind.DefaultPush))).ToList();
    }

    public async Task SetAsync(KindInfo kind, bool push)
    {
        PreferenceItem preference = new() { Kind = kind.Key, Push = push };
        if (access.SignedIn)
        {
            await outbox.EnqueueAsync(SetMethod, preference, NotificationsContractsModel.Instance);
            return;
        }

        await local.SetAsync(OnThisPhone + kind.Key, push ? "on" : "off");
        if (await bridge.ProveInstallAsync())
        {
            try
            {
                await _service.SetPhonePreferenceAsync(preference);
            }
            catch (RpcException)
            {
            }
        }
    }

    private bool MayReceive(KindInfo kind) =>
        kind.ForNode is not { } node
        || (ScopeKind.TryParse(kind.ForScope ?? "", out ScopeKind scope) && access.CanSomewhere(new Permission(node, scope, "")));

    private async Task<Dictionary<string, bool>> OnPhoneAsync(List<KindInfo> kinds)
    {
        Dictionary<string, bool> chosen = [];
        foreach (KindInfo kind in kinds)
        {
            if (await local.GetAsync(OnThisPhone + kind.Key) is { } value)
                chosen[kind.Key] = value == "on";
        }
        return chosen;
    }

    private static T Decode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(byte[] payload) => NotificationsContractsModel.Instance.Deserialize<T>(new MemoryStream(payload));
}
