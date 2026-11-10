using System.Diagnostics.CodeAnalysis;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Platform.Client.Outbox;

public interface IWrites
{
    bool Queued { get; }

    ValueTask SendAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request, TypeModel model);
}

public sealed class QueuedWrites(IOutbox outbox) : IWrites
{
    public bool Queued => true;

    public async ValueTask SendAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request, TypeModel model) =>
        await outbox.EnqueueAsync(method, request, model);
}
