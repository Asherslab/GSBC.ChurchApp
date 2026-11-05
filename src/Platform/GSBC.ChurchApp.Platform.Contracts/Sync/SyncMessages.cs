using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Sync;

[ProtoContract]
public sealed class FeedCursor
{
    [ProtoMember(1)] public string Feed { get; set; } = "";
    [ProtoMember(2)] public long Cursor { get; set; }
    [ProtoMember(3)] public bool Latest { get; set; }
}

[ProtoContract]
public sealed class PullRequest
{
    [ProtoMember(1)] public List<FeedCursor> Cursors { get; set; } = [];
    [ProtoMember(2)] public string? Origin { get; set; }
}

[ProtoContract]
public sealed class PullReply
{
    [ProtoMember(1)] public List<FeedBatch> Feeds { get; set; } = [];
    // The same number as WatchEvent.Origin: the native layer reads both replies with one decoder.
    [ProtoMember(3)] public string? Origin { get; set; }
}

[ProtoContract]
public sealed class WatchRequest
{
    [ProtoMember(1)] public List<FeedCursor> Cursors { get; set; } = [];
    [ProtoMember(2)] public string? Origin { get; set; }
}

[ProtoContract]
public sealed class WatchEvent
{
    [ProtoMember(1)] public List<FeedBatch> Feeds { get; set; } = [];
    [ProtoMember(2)] public bool IsHeartbeat { get; set; }
    [ProtoMember(3)] public string? Origin { get; set; }
}

[ProtoContract]
public sealed class FeedBatch
{
    [ProtoMember(1)] public string Feed { get; set; } = "";
    [ProtoMember(2)] public long Cursor { get; set; }
    [ProtoMember(3)] public bool Reset { get; set; }
    [ProtoMember(4)] public List<FeedChange> Changes { get; set; } = [];
    [ProtoMember(5)] public bool HasMore { get; set; }
}

[ProtoContract]
public sealed class FeedChange
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public long Version { get; set; }
    [ProtoMember(3)] public bool Deleted { get; set; }
    [ProtoMember(4)] public byte[] Payload { get; set; } = [];
    [ProtoMember(5)] public string? SortKey { get; set; }
    [ProtoMember(6)] public DateTime? StartsAt { get; set; }
    [ProtoMember(7)] public DateTime? EndsAt { get; set; }
    [ProtoMember(8)] public string? GroupKey { get; set; }
}
