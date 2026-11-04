using ProtoBuf;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public enum DraftStatus
{
    Open = 0,
    WaitingToPublish = 1,
}

[ProtoContract]
public sealed class ValueChange
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public string? Value { get; set; }
}

[ProtoContract]
public sealed class PatchMessage
{
    [ProtoMember(1)] public string PlacementId { get; set; } = "";
    [ProtoMember(2)] public string Area { get; set; } = "";
    [ProtoMember(3)] public string? ScopeId { get; set; }
    [ProtoMember(4)] public string Surface { get; set; } = "";
    [ProtoMember(5)] public int Kind { get; set; }
    [ProtoMember(6)] public int? Order { get; set; }
    [ProtoMember(7)] public bool? Hidden { get; set; }
    [ProtoMember(8)] public string? Audience { get; set; }
    [ProtoMember(9)] public bool SetsDates { get; set; }
    [ProtoMember(10)] public DateTime? StartsAt { get; set; }
    [ProtoMember(11)] public DateTime? EndsAt { get; set; }
    [ProtoMember(12)] public List<ValueChange> Values { get; set; } = [];
    [ProtoMember(13)] public Placement? Base { get; set; }
}

[ProtoContract]
public sealed class Draft
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string AuthorId { get; set; } = "";
    [ProtoMember(4)] public DateTime CreatedAt { get; set; }
    [ProtoMember(5)] public DateTime UpdatedAt { get; set; }
    [ProtoMember(6)] public DraftStatus Status { get; set; }
    [ProtoMember(7)] public bool LayerOn { get; set; }
    [ProtoMember(8)] public DateTime? LayerStartsAt { get; set; }
    [ProtoMember(9)] public DateTime? LayerEndsAt { get; set; }
    [ProtoMember(10)] public long Revision { get; set; }
    [ProtoMember(11)] public List<PatchMessage> Changes { get; set; } = [];
    [ProtoMember(12)] public List<string> SharedWith { get; set; } = [];
    [ProtoMember(13)] public List<string> DroppedFromLayer { get; set; } = [];
    [ProtoMember(14)] public bool Mine { get; set; }
    [ProtoMember(15)] public int ToCheck { get; set; }
}

[ProtoContract]
public sealed class PublishedDraft
{
    [ProtoMember(1)] public string BatchId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public DateTime At { get; set; }
    [ProtoMember(4)] public string By { get; set; } = "";
    [ProtoMember(5)] public List<string> Areas { get; set; } = [];
    [ProtoMember(6)] public int Changes { get; set; }
    [ProtoMember(7)] public string? UndoneBatchId { get; set; }
    [ProtoMember(8)] public DateTime? UndoneAt { get; set; }
    [ProtoMember(9)] public string? UndoneBy { get; set; }
}

[ProtoContract]
public sealed class DraftList
{
    [ProtoMember(1)] public List<Draft> Drafts { get; set; } = [];
    [ProtoMember(2)] public List<PublishedDraft> Published { get; set; } = [];
    [ProtoMember(3)] public bool SeesEveryDraft { get; set; }
}

[ProtoContract]
public sealed class EventLayerMessage
{
    [ProtoMember(1)] public string DraftId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public DateTime? StartsAt { get; set; }
    [ProtoMember(4)] public DateTime? EndsAt { get; set; }
    [ProtoMember(5)] public List<PatchMessage> Changes { get; set; } = [];
}

[ProtoContract]
public sealed class ListDraftsRequest;

[ProtoContract]
public sealed class DraftQuery
{
    [ProtoMember(1)] public string DraftId { get; set; } = "";
}

[ProtoContract]
public sealed class NewDraftRequest
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public string? CopyOf { get; set; }
}

[ProtoContract]
public sealed class RenameDraftRequest
{
    [ProtoMember(1)] public string DraftId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class SaveDraftRequest
{
    [ProtoMember(1)] public string DraftId { get; set; } = "";
    [ProtoMember(2)] public List<PatchMessage> Set { get; set; } = [];
    [ProtoMember(3)] public List<string> Cleared { get; set; } = [];
}

[ProtoContract]
public sealed class SettleRequest
{
    [ProtoMember(1)] public string DraftId { get; set; } = "";
    [ProtoMember(2)] public string PlacementId { get; set; } = "";
    [ProtoMember(3)] public bool KeepMine { get; set; }
}

[ProtoContract]
public sealed class EventLayerRequest
{
    [ProtoMember(1)] public string DraftId { get; set; } = "";
    [ProtoMember(2)] public DateTime? StartsAt { get; set; }
    [ProtoMember(3)] public DateTime? EndsAt { get; set; }
}

[ProtoContract]
public sealed class ShareRequest
{
    [ProtoMember(1)] public string DraftId { get; set; } = "";
    [ProtoMember(2)] public string UserId { get; set; } = "";
}

[ProtoContract]
public sealed class PublishReply
{
    [ProtoMember(1)] public string BatchId { get; set; } = "";
}

[ProtoContract]
public sealed class DraftsDone;
