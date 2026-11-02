using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Media;

[ProtoContract]
public sealed class MediaRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Key { get; set; } = "";
    [ProtoMember(3)] public string ContentType { get; set; } = "";
    [ProtoMember(4)] public long Bytes { get; set; }
    [ProtoMember(5)] public int Width { get; set; }
    [ProtoMember(6)] public int Height { get; set; }
    [ProtoMember(7)] public string? Url { get; set; }
    [ProtoMember(8)] public bool IsPrivate { get; set; }
    [ProtoMember(9)] public string AltText { get; set; } = "";
    [ProtoMember(10)] public string FileName { get; set; } = "";

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.Ordinal);
}
