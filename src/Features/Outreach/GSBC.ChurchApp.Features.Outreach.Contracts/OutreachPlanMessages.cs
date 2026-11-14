using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public enum PlanTarget
{
    Letterboxes = 0,
    Bundles = 1,
    Minutes = 2,
}

[ProtoContract]
public sealed class PlannedRoute
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public int Colour { get; set; }
    [ProtoMember(4)] public bool Locked { get; set; }
    [ProtoMember(5)] public GeoPoint? Start { get; set; }
    [ProtoMember(6)] public List<Stretch> Stretches { get; set; } = [];
    [ProtoMember(7)] public string RouteId { get; set; } = "";
}

[ProtoContract]
public sealed class DayDraft
{
    [ProtoMember(1)] public List<GeoPoint> Area { get; set; } = [];
    [ProtoMember(2)] public string AreaId { get; set; } = "";
    [ProtoMember(3)] public PlanTarget Target { get; set; }
    [ProtoMember(4)] public double PerRoute { get; set; }
    [ProtoMember(5)] public bool RestedToo { get; set; }
    [ProtoMember(6)] public List<PlannedRoute> Routes { get; set; } = [];
    [ProtoMember(7)] public string Region { get; set; } = "";
    [ProtoMember(8)] public DateTime ChangedAt { get; set; }
}

[ProtoContract]
public sealed class DayPlan
{
    [ProtoMember(1, IsRequired = true)] public OutreachDay Day { get; set; } = new();
    [ProtoMember(2)] public List<DayRoute> Routes { get; set; } = [];
    [ProtoMember(3)] public DayDraft? Draft { get; set; }
    [ProtoMember(4)] public DateTime? PublishedAt { get; set; }
}

[ProtoContract]
public sealed class DraftSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string DayId { get; set; } = "";
    [ProtoMember(3)] public DayDraft? Draft { get; set; }
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class RoutesPublish : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string DayId { get; set; } = "";
    [ProtoMember(3)] public List<DayRoute> Routes { get; set; } = [];
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class Published
{
    [ProtoMember(1)] public List<DayRoute> Routes { get; set; } = [];
}

[ProtoContract]
public sealed class DayDeletion
{
    [ProtoMember(1)] public int Routes { get; set; }
    [ProtoMember(2)] public int Claims { get; set; }
    [ProtoMember(3)] public int Walks { get; set; }
    [ProtoMember(4)] public bool OnNow { get; set; }
}

[ProtoContract]
public sealed class PlanningMarks
{
    [ProtoMember(1)] public List<Skip> Skips { get; set; } = [];
    [ProtoMember(2)] public List<Exclusion> Exclusions { get; set; } = [];
}

[ProtoContract]
public sealed class DayFacts
{
    [ProtoMember(1)] public string DayId { get; set; } = "";
    [ProtoMember(2)] public int Routes { get; set; }
    [ProtoMember(3)] public int Letterboxes { get; set; }
    [ProtoMember(4)] public int Claims { get; set; }
    [ProtoMember(5)] public DateTime? RoutesPublishedAt { get; set; }
    [ProtoMember(6)] public bool HasDraft { get; set; }
}
