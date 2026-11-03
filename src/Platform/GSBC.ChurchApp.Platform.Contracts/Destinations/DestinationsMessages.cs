using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Destinations;

public enum DestinationState
{
    Available = 0,
    NotForYou = 1,
    Gone = 2,
    Unknown = 3,
}

[ProtoContract]
public sealed class Destination
{
    [ProtoMember(1)] public string Path { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public string Where { get; set; } = "";
    [ProtoMember(4)] public string Icon { get; set; } = "";
    [ProtoMember(5)] public string Area { get; set; } = "";
    [ProtoMember(6)] public string Audience { get; set; } = "";
    [ProtoMember(7)] public string? AudienceScope { get; set; }
    [ProtoMember(8)] public string WhoCanOpen { get; set; } = "";
    [ProtoMember(9)] public DateTime? When { get; set; }
    [ProtoMember(10)] public DestinationState State { get; set; }
    [ProtoMember(11)] public string? OpenUrl { get; set; }
    [ProtoMember(12)] public string Kind { get; set; } = "";
}

[ProtoContract]
public sealed class DestinationSearch
{
    [ProtoMember(1)] public string Text { get; set; } = "";
    [ProtoMember(2)] public string? Area { get; set; }
    [ProtoMember(3)] public string? OpenableBy { get; set; }
    [ProtoMember(4)] public int Take { get; set; }
}

[ProtoContract]
public sealed class DestinationGroup
{
    [ProtoMember(1)] public string Area { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public string Icon { get; set; } = "";
    [ProtoMember(4)] public List<Destination> Destinations { get; set; } = [];
    [ProtoMember(5)] public bool HasMore { get; set; }
}

[ProtoContract]
public sealed class DestinationResults
{
    [ProtoMember(1)] public List<DestinationGroup> Groups { get; set; } = [];
}

[ProtoContract]
public sealed class DestinationRequest
{
    [ProtoMember(1)] public string Path { get; set; } = "";
}
