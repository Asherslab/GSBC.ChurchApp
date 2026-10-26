using System.Runtime.Serialization;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf;
using ProtoBuf.Grpc;

namespace Contracts;

public enum EventKind { Service, Social, Youth }

[ProtoContract]
public sealed class ChurchEvent
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public DateTime StartsAt { get; set; }
    [ProtoMember(4)] public EventKind Kind { get; set; }
    [ProtoMember(5)] public List<string> Tags { get; set; } = [];
    [ProtoMember(6)] public Venue? Venue { get; set; }
    [ProtoMember(7)] public decimal? Cost { get; set; }
}

[ProtoContract]
public sealed class Venue
{
    [ProtoMember(1)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class EventsRequest
{
    [ProtoMember(1)] public int Take { get; set; }
}

[ProtoContract]
public sealed class EventsReply
{
    [ProtoMember(1)] public List<ChurchEvent> Events { get; set; } = [];
}

[Service]
public interface IEventsService
{
    [Operation] Task<EventsReply> ListAsync(EventsRequest request, CallContext context = default);
    [Operation] IAsyncEnumerable<ChurchEvent> WatchAsync(EventsRequest request, CallContext context = default);
}
