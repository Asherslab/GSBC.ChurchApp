using ProtoBuf;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public enum UsageGroup
{
    Everyone = 0,
    NotSignedIn = 1,
    SignedIn = 2,
    OnATeam = 3,
}

[ProtoContract]
public sealed class UsageReport
{
    public const int MaximumCounts = 500;

    [ProtoMember(1)] public ReportContext? Context { get; set; }
    [ProtoMember(2)] public List<UsageCount> Counts { get; set; } = [];
}

[ProtoContract]
public sealed class UsageCount
{
    [ProtoMember(1)] public int Day { get; set; }
    [ProtoMember(2)] public string Measure { get; set; } = "";
    [ProtoMember(3)] public string Subject { get; set; } = "";
    [ProtoMember(4)] public UsageGroup Group { get; set; }
    [ProtoMember(5)] public int Count { get; set; }
}
