using System.Text.Json;
using System.Text.Json.Serialization;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Signals;

namespace GSBC.ChurchApp.Features.Feedback.Client;

internal sealed record FeedState(string Feed, int Rows, long Bytes, long Cursor);

internal sealed record WaitingState(string Method, DateTime CreatedAt, int Attempts, bool Failed, bool WaitsForUpload, string? LastError);

internal sealed record PhoneStateReport(
    DateTime At, string Sync, bool Online, bool SignedIn, bool DevelopmentBuild, string? Gateway, long StoreBytes,
    IReadOnlyList<FeedState> Feeds, IReadOnlyList<WaitingState> Waiting, string TimeZone, string Culture);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(PhoneStateReport))]
internal sealed partial class PhoneStateJson : JsonSerializerContext;

public sealed class PhoneState(ILocalFeeds feeds, IOutbox outbox, IAccess access, ClientSignals signals, NativeBridge bridge, TimeProvider time)
{
    public async Task<string> DescribeAsync()
    {
        StoreInfo store = feeds.IsAvailable ? await feeds.InfoAsync() : new StoreInfo(0, []);
        IReadOnlyList<OutboxEntry> waiting = outbox.IsAvailable ? await outbox.ListAsync() : [];
        PhoneStateReport report = new(
            time.GetUtcNow().UtcDateTime, feeds.State.ToString(), signals.Online, access.SignedIn, bridge.IsDevelopmentBuild, bridge.Gateway?.ToString(),
            store.FileBytes,
            store.Feeds.Select(feed => new FeedState(feed.Feed, feed.Rows, feed.Bytes, feed.Cursor)).ToList(),
            waiting.Select(entry => new WaitingState(entry.Method, entry.CreatedAt, entry.Attempts, entry.Failed, entry.WaitsForUpload, entry.LastError)).ToList(),
            TimeZoneInfo.Local.Id, System.Globalization.CultureInfo.CurrentCulture.Name);
        return JsonSerializer.Serialize(report, PhoneStateJson.Default.PhoneStateReport);
    }
}
