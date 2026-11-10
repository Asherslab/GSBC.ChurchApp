using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.People.Contracts;

[ProtoContract]
public sealed class SmcStatusRequest;

[ProtoContract]
public sealed class SmcSyncRequest;

[ProtoContract]
public sealed class SmcUnmatchedRequest;

[ProtoContract]
public sealed class SmcSyncStatus
{
    [ProtoMember(1)] public bool Connected { get; set; }
    [ProtoMember(2)] public DateTime? LastRunAt { get; set; }
    [ProtoMember(3)] public DateTime? LastSucceededAt { get; set; }
    [ProtoMember(4)] public string? Problem { get; set; }
    [ProtoMember(5)] public int People { get; set; }
    [ProtoMember(6)] public int Linked { get; set; }
    [ProtoMember(7)] public int ToMatch { get; set; }
    [ProtoMember(8)] public int ToCheck { get; set; }
    [ProtoMember(9)] public DateTime? NextRunAt { get; set; }
}

[ProtoContract]
public sealed class SmcPeopleList
{
    [ProtoMember(1)] public List<SmcPerson> People { get; set; } = [];
}

[ProtoContract]
public sealed class SmcPerson
{
    [ProtoMember(1)] public string SmcId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Email { get; set; } = "";
    [ProtoMember(4)] public DateTime? BirthDate { get; set; }
    [ProtoMember(5)] public string Why { get; set; } = "";
}

[ProtoContract]
public sealed class SmcLinkRequest : IScopedRequest
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
    [ProtoMember(2)] public string SmcId { get; set; } = "";

    public string ScopeId => PersonId;
}
