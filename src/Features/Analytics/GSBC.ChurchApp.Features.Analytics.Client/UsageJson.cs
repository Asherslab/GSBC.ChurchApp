using System.Text.Json.Serialization;
using GSBC.ChurchApp.Features.Analytics.Contracts;

namespace GSBC.ChurchApp.Features.Analytics.Client;

internal sealed class UsageState
{
    public OpeningMemory Opened { get; set; } = new();
    public StateMemory States { get; set; } = new();
    public int WeekGroup { get; set; }
    public long LastReportAt { get; set; }
    public List<PendingCount> Pending { get; set; } = [];
}

internal sealed class PendingCount
{
    public int Day { get; set; }
    public string Measure { get; set; } = "";
    public string Subject { get; set; } = "";
    public int Group { get; set; }
    public int Count { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UsageState))]
internal sealed partial class UsageJson : JsonSerializerContext;
