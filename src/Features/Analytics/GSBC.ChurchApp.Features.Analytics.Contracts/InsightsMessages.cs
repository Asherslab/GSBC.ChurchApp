using ProtoBuf;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

[ProtoContract]
public sealed class Tally
{
    public const int SmallestShown = 5;

    [ProtoMember(1)] public long Value { get; set; }
    [ProtoMember(2)] public bool FewerThanFive { get; set; }

    public static Tally Of(long count) => count is > 0 and < SmallestShown ? new Tally { FewerThanFive = true } : new Tally { Value = count };
}

[ProtoContract]
public sealed class InsightsQuery;

[ProtoContract]
public sealed class InsightsSummary
{
    [ProtoMember(1)] public int Today { get; set; }
    [ProtoMember(2)] public int WeekStart { get; set; }
    [ProtoMember(3)] public DateTime? LastReportAt { get; set; }
    [ProtoMember(4)] public Tally? ThisWeek { get; set; }
    [ProtoMember(5)] public Tally? LastWeek { get; set; }
    [ProtoMember(6)] public Tally? ThisMonth { get; set; }
    [ProtoMember(7)] public Tally? NewThisMonth { get; set; }
    [ProtoMember(8)] public Tally? NewLastMonth { get; set; }
    [ProtoMember(9)] public List<GroupTally> Groups { get; set; } = [];
    [ProtoMember(10)] public List<WeekPoint> Weeks { get; set; } = [];
    [ProtoMember(11)] public List<WeekPoint> WeeksLastYear { get; set; } = [];
    [ProtoMember(12)] public List<TopList> Top { get; set; } = [];
    [ProtoMember(13)] public List<LabelTally> Platforms { get; set; } = [];
    [ProtoMember(14)] public List<LabelTally> Versions { get; set; } = [];
    [ProtoMember(15)] public Tally? OnLatestVersion { get; set; }
    [ProtoMember(16)] public string LatestVersion { get; set; } = "";
    [ProtoMember(17)] public Tally? ReturningThisWeek { get; set; }
    [ProtoMember(18)] public List<TopList> States { get; set; } = [];
}

[ProtoContract]
public sealed class GroupTally
{
    [ProtoMember(1)] public UsageGroup Group { get; set; }
    [ProtoMember(2)] public Tally? Phones { get; set; }
}

[ProtoContract]
public sealed class WeekPoint
{
    [ProtoMember(1)] public int WeekStart { get; set; }
    [ProtoMember(2)] public Tally? Phones { get; set; }
}

[ProtoContract]
public sealed class TopList
{
    [ProtoMember(1)] public string Measure { get; set; } = "";
    [ProtoMember(2)] public string Label { get; set; } = "";
    [ProtoMember(3)] public string Counts { get; set; } = "";
    [ProtoMember(4)] public List<LabelTally> Entries { get; set; } = [];
}

[ProtoContract]
public sealed class LabelTally
{
    [ProtoMember(1)] public string Label { get; set; } = "";
    [ProtoMember(2)] public Tally? Count { get; set; }
}
