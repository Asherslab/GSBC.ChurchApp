using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Native;
using Microsoft.JSInterop;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Platform.Client.Outbox;

public sealed class NativeOutbox(NativeBridge bridge, IJSRuntime js) : IOutbox, IDisposable
{
    private DotNetObjectReference<NativeOutbox>? _receiver;

    public bool IsAvailable => bridge.IsNative;

    public event Action? Changed;

    public async Task StartAsync()
    {
        if (!IsAvailable)
            return;

        _receiver = DotNetObjectReference.Create(this);
        await js.InvokeVoidAsync("gsbcNative.listenOutbox", _receiver);
    }

    public async ValueTask<string> EnqueueAsync(string method, byte[] request) =>
        IsAvailable
            ? await js.InvokeAsync<string>("gsbcNative.enqueue", method, Convert.ToBase64String(request), false)
            : throw new InvalidOperationException("Queued writes need the phone app's data layer.");

    public async ValueTask EnqueueQuietlyAsync(string method, byte[] request)
    {
        if (IsAvailable)
            await js.InvokeAsync<string>("gsbcNative.enqueue", method, Convert.ToBase64String(request), true);
    }

    public async ValueTask DiscardQuietAsync()
    {
        if (IsAvailable)
            await js.InvokeVoidAsync("gsbcNative.discardQuietOutbox");
    }

    public ValueTask<string> EnqueueAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request, TypeModel model)
    {
        using MemoryStream body = new();
        model.Serialize(body, request);
        return EnqueueAsync(method, body.ToArray());
    }

    public async ValueTask<IReadOnlyList<OutboxEntry>> ListAsync()
    {
        if (!IsAvailable)
            return [];

        return (JsonSerializer.Deserialize(await js.InvokeAsync<string>("gsbcNative.outbox"), NativeJson.Default.OutboxEntryJsonArray) ?? [])
            .Select(entry => new OutboxEntry(entry.Id, entry.Method, DateTime.UnixEpoch.AddMilliseconds(entry.CreatedAt),
                entry.Attempts, entry.LastError, entry.Failed))
            .ToList();
    }

    public async ValueTask RetryAsync(string id) => await js.InvokeVoidAsync("gsbcNative.retryOutbox", id);

    public async ValueTask DiscardAsync(string id) => await js.InvokeVoidAsync("gsbcNative.discardOutbox", id);

    [JSInvokable]
    public void OnOutboxChanged() => Changed?.Invoke();

    public void Dispose() => _receiver?.Dispose();
}
