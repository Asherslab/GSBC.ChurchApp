using ProtoBuf;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

[ProtoContract]
public sealed class SettingValue
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public string Value { get; set; } = "";
}

[ProtoContract]
public sealed class Placement
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Area { get; set; } = "";
    [ProtoMember(3)] public string? ScopeId { get; set; }
    [ProtoMember(4)] public string Surface { get; set; } = "";
    [ProtoMember(5)] public int Order { get; set; }
    [ProtoMember(6)] public bool Hidden { get; set; }
    [ProtoMember(7)] public string Audience { get; set; } = "";
    [ProtoMember(8)] public DateTime? StartsAt { get; set; }
    [ProtoMember(9)] public DateTime? EndsAt { get; set; }
    [ProtoMember(10)] public List<SettingValue> Values { get; set; } = [];
    [ProtoMember(11)] public long Revision { get; set; }
    [ProtoMember(12)] public DateTime UpdatedAt { get; set; }
    [ProtoMember(13)] public string? UpdatedBy { get; set; }
}

[ProtoContract]
public sealed class AreaQuery
{
    [ProtoMember(1)] public string Area { get; set; } = "";
    [ProtoMember(2)] public string? ScopeId { get; set; }
}

[ProtoContract]
public sealed class AreaPlacements
{
    [ProtoMember(1)] public List<Placement> Placements { get; set; } = [];
}

[ProtoContract]
public sealed class ChangeList
{
    [ProtoMember(1)] public List<ChangeBatch> Batches { get; set; } = [];
}

[ProtoContract]
public sealed class ChangeBatch
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public DateTime At { get; set; }
    [ProtoMember(3)] public string By { get; set; } = "";
    [ProtoMember(4)] public List<PlacementChange> Changes { get; set; } = [];
    [ProtoMember(5)] public string? RollsBack { get; set; }
    [ProtoMember(6)] public string? DraftName { get; set; }
}

[ProtoContract]
public sealed class PlacementChange
{
    [ProtoMember(1)] public string PlacementId { get; set; } = "";
    [ProtoMember(2)] public string Surface { get; set; } = "";
    [ProtoMember(3)] public Placement? Before { get; set; }
    [ProtoMember(4)] public Placement? After { get; set; }
}

[ProtoContract]
public sealed class RollBackRequest
{
    [ProtoMember(1)] public string BatchId { get; set; } = "";
}
