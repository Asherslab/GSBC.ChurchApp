using ProtoBuf;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

[ProtoContract]
public sealed class HealthQuery
{
    [ProtoMember(1)] public int Days { get; set; }
}

[ProtoContract]
public sealed class HealthOverview
{
    [ProtoMember(1)] public string Environment { get; set; } = "";
    [ProtoMember(2)] public List<ErrorKindSummary> Kinds { get; set; } = [];
    [ProtoMember(3)] public List<FailedCallSummary> FailedCalls { get; set; } = [];
}

[ProtoContract]
public sealed class ErrorKindSummary
{
    [ProtoMember(1)] public Guid Id { get; set; }
    [ProtoMember(2)] public ReportOrigin Origin { get; set; }
    [ProtoMember(3)] public bool NativeCrash { get; set; }
    [ProtoMember(4)] public string ExceptionType { get; set; } = "";
    [ProtoMember(5)] public string Where { get; set; } = "";
    [ProtoMember(6)] public long Count { get; set; }
    [ProtoMember(7)] public DateTime FirstSeen { get; set; }
    [ProtoMember(8)] public DateTime LastSeen { get; set; }
    [ProtoMember(9)] public List<string> AppVersions { get; set; } = [];
    [ProtoMember(10)] public bool OnlyDevelopmentBuilds { get; set; }
}

[ProtoContract]
public sealed class FailedCallSummary
{
    [ProtoMember(1)] public ReportOrigin Origin { get; set; }
    [ProtoMember(2)] public string Method { get; set; } = "";
    [ProtoMember(3)] public int Status { get; set; }
    [ProtoMember(4)] public long Count { get; set; }
    [ProtoMember(5)] public int LastDay { get; set; }
}

[ProtoContract]
public sealed class ErrorKindRequest
{
    [ProtoMember(1)] public Guid Id { get; set; }
}

[ProtoContract]
public sealed class ErrorKindDetail
{
    [ProtoMember(1)] public ErrorKindSummary? Kind { get; set; }
    [ProtoMember(2)] public List<string> Frames { get; set; } = [];
    [ProtoMember(3)] public List<CountByLabel> Routes { get; set; } = [];
    [ProtoMember(4)] public List<CountByLabel> Platforms { get; set; } = [];
    [ProtoMember(5)] public List<CountByLabel> Builds { get; set; } = [];
    [ProtoMember(6)] public List<CountByDay> Days { get; set; } = [];
}

[ProtoContract]
public sealed class CountByLabel
{
    [ProtoMember(1)] public string Label { get; set; } = "";
    [ProtoMember(2)] public long Count { get; set; }
}

[ProtoContract]
public sealed class CountByDay
{
    [ProtoMember(1)] public int Day { get; set; }
    [ProtoMember(2)] public long Count { get; set; }
}
