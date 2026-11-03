using GSBC.ChurchApp.Features.Analytics.Contracts;

namespace GSBC.ChurchApp.Features.Analytics.Api.Data;

public sealed class ErrorKindRecord
{
    public Guid Id { get; set; }
    public required string Fingerprint { get; set; }
    public ReportOrigin Origin { get; set; }
    public bool NativeCrash { get; set; }
    public required string ExceptionType { get; set; }
    public List<string> Frames { get; set; } = [];
    public required string Where { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public long Count { get; set; }
    public DateTime? NotifiedAt { get; set; }
}

public sealed class ErrorReportRecord
{
    public long Id { get; set; }
    public Guid KindId { get; set; }
    public DateOnly Day { get; set; }
    public DevicePlatform Platform { get; set; }
    public string AppVersion { get; set; } = "";
    public string AppBuild { get; set; } = "";
    public int OsMajor { get; set; }
    public bool DevelopmentBuild { get; set; }
    public string Route { get; set; } = "";
    public int Count { get; set; }
    public DateTime ReceivedAt { get; set; }
}

public sealed class FailedCallRecord
{
    public DateOnly Day { get; set; }
    public ReportOrigin Origin { get; set; }
    public required string Method { get; set; }
    public int Status { get; set; }
    public string AppVersion { get; set; } = "";
    public long Count { get; set; }
}

public sealed class DailyCountRecord
{
    public DateOnly Day { get; set; }
    public required string Measure { get; set; }
    public string Subject { get; set; } = "";
    public UsageGroup Group { get; set; }
    public DevicePlatform Platform { get; set; }
    public string AppVersion { get; set; } = "";
    public long Count { get; set; }
}

public sealed class MonthlyCountRecord
{
    public DateOnly Month { get; set; }
    public required string Measure { get; set; }
    public string Subject { get; set; } = "";
    public UsageGroup Group { get; set; }
    public DevicePlatform Platform { get; set; }
    public string AppVersion { get; set; } = "";
    public long Count { get; set; }
}

public sealed class SeenReportRecord
{
    public required string RequestId { get; set; }
    public DateTime SeenAt { get; set; }
}
