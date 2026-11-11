using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Notifications;

[Service("gsbc.notifications.DigestSchedules")]
public interface IDigestSchedulesService
{
    [SignedIn]
    ValueTask<DigestScheduleMessage> GetAsync(DigestScheduleRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<DigestScheduleMessage> ChangeAsync(DigestScheduleChange request, CallContext context = default);
}

[ProtoContract]
public sealed class DigestScheduleRequest
{
    [ProtoMember(1)] public string Kind { get; set; } = "";
}

[ProtoContract]
public sealed class DigestScheduleChange
{
    [ProtoMember(1)] public string Kind { get; set; } = "";
    [ProtoMember(2)] public List<int> AfterMinutes { get; set; } = [];
    [ProtoMember(3)] public int? DailyAtMinute { get; set; }
}

[ProtoContract]
public sealed class DigestScheduleMessage
{
    [ProtoMember(1)] public string Kind { get; set; } = "";
    [ProtoMember(2)] public List<int> AfterMinutes { get; set; } = [];
    [ProtoMember(3)] public int? DailyAtMinute { get; set; }
    [ProtoMember(4)] public List<int> DefaultAfterMinutes { get; set; } = [];
    [ProtoMember(5)] public int? DefaultDailyAtMinute { get; set; }
    [ProtoMember(6)] public bool MayChange { get; set; }
}
