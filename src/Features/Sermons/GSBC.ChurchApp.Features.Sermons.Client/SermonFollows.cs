using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Notifications;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Signals;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed class SermonFollows(ChannelBase channel, ILocalPreferences preferences, ClientSignals signals) : IFollowedScopes, IClientStartupTask, IDisposable
{
    private const string Remembered = "sermons.follows";
    private readonly ISermonsService _sermons = channel.CreateGrpcService<ISermonsService>();
    private HashSet<string> _scopes = [];

    public event Action? Changed;

    public IReadOnlySet<string> Scopes => _scopes;

    public bool NewSermons => _scopes.Contains(SermonsNotifications.AllSermonsScope);

    public bool Series(string seriesId) => _scopes.Contains(seriesId);

    public async Task RunAsync()
    {
        _scopes = (await preferences.GetAsync(Remembered) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        signals.Changed += OnSignals;
        _ = RefreshAsync();
    }

    public async Task<string?> SetAsync(string? seriesId, bool follow)
    {
        try
        {
            await TakeAsync(await _sermons.FollowAsync(new FollowRequest { SeriesId = seriesId, Follow = follow }));
            return null;
        }
        catch (RpcException exception) when (exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            return "That needs a connection. Try again when you're online.";
        }
        catch (RpcException exception)
        {
            return exception.Status.Detail;
        }
    }

    public void Dispose() => signals.Changed -= OnSignals;

    private async Task RefreshAsync()
    {
        try
        {
            await TakeAsync(await _sermons.FollowsAsync(new Done()));
        }
        catch (RpcException)
        {
        }
    }

    private async Task TakeAsync(Follows follows)
    {
        HashSet<string> scopes = [.. follows.SeriesIds];
        if (follows.NewSermons)
            scopes.Add(SermonsNotifications.AllSermonsScope);
        _scopes = scopes;
        await preferences.SetAsync(Remembered, string.Join(',', scopes));
        Changed?.Invoke();
    }

    private void OnSignals(ClientChange change)
    {
        if (change.Concerns(ChangeKind.Session | ChangeKind.Reconnected, null))
            _ = RefreshAsync();
    }
}
