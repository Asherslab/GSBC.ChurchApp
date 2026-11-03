using ProtoBuf;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

[ProtoContract]
public sealed class DiagnosticsReport
{
    public const int MaximumErrors = 50;
    public const int MaximumFailedCalls = 100;

    [ProtoMember(1)] public ReportContext? Context { get; set; }
    [ProtoMember(2)] public List<ErrorOccurrence> Errors { get; set; } = [];
    [ProtoMember(3)] public List<FailedCallCount> FailedCalls { get; set; } = [];
}

[ProtoContract]
public sealed class ErrorOccurrence
{
    [ProtoMember(1)] public int Day { get; set; }
    [ProtoMember(2)] public string ExceptionType { get; set; } = "";
    [ProtoMember(3)] public List<string> Frames { get; set; } = [];
    [ProtoMember(4)] public string Route { get; set; } = "";
    [ProtoMember(5)] public int Count { get; set; }
    [ProtoMember(6)] public bool NativeCrash { get; set; }
    [ProtoMember(7)] public string? AppVersion { get; set; }
    [ProtoMember(8)] public string? AppBuild { get; set; }
}

[ProtoContract]
public sealed class FailedCallCount
{
    [ProtoMember(1)] public int Day { get; set; }
    [ProtoMember(2)] public string Method { get; set; } = "";
    [ProtoMember(3)] public int Status { get; set; }
    [ProtoMember(4)] public int Count { get; set; }
}
