using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Client.Uploads;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Platform.Client.Outbox;

public sealed record OutboxEntry(string Id, string Method, DateTime CreatedAt, int Attempts, string? LastError, bool Failed, bool WaitsForUpload = false, string? FileName = null);

public interface IOutbox
{
    bool IsAvailable { get; }

    event Action? Changed;

    ValueTask<string> EnqueueAsync(string method, byte[] request);

    ValueTask<string> EnqueueAsync(string method, byte[] request, IReadOnlyList<QueuedUpload> uploads) =>
        uploads.Count == 0 ? EnqueueAsync(method, request) : throw new NotSupportedException("Queued uploads need the phone app's data layer.");

    ValueTask<string> EnqueueAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request, TypeModel model);

    ValueTask EnqueueQuietlyAsync(string method, byte[] request);

    ValueTask DiscardQuietAsync();

    ValueTask<IReadOnlyList<OutboxEntry>> ListAsync();

    ValueTask RetryAsync(string id);

    ValueTask DiscardAsync(string id);
}
