using ProtoBuf;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

public enum ReportOrigin
{
    Unknown = 0,
    App = 1,
    Admin = 2,
    Server = 3,
}

public enum DevicePlatform
{
    Unknown = 0,
    Ios = 1,
    Android = 2,
    Web = 3,
}

[ProtoContract]
public sealed class ReportContext
{
    [ProtoMember(1)] public ReportOrigin Origin { get; set; }
    [ProtoMember(2)] public DevicePlatform Platform { get; set; }
    [ProtoMember(3)] public string AppVersion { get; set; } = "";
    [ProtoMember(4)] public string AppBuild { get; set; } = "";
    [ProtoMember(5)] public int OsMajor { get; set; }
    [ProtoMember(6)] public bool DevelopmentBuild { get; set; }
}

[ProtoContract]
public sealed class Received;
