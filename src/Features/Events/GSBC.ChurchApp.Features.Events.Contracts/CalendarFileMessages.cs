using ProtoBuf;

namespace GSBC.ChurchApp.Features.Events.Contracts;

[ProtoContract]
public sealed class CalendarFileRequest
{
    [ProtoMember(1)] public string Calendar { get; set; } = "";
    [ProtoMember(2)] public string Secret { get; set; } = "";
}

[ProtoContract]
public sealed class CalendarFile
{
    [ProtoMember(1)] public bool Found { get; set; }
    [ProtoMember(2)] public string Text { get; set; } = "";
}

[ProtoContract]
public sealed class CalendarLink
{
    [ProtoMember(1)] public string Secret { get; set; } = "";
}
