using GSBC.ChurchApp.Platform.Contracts.Media;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

[ProtoContract]
public sealed class RichText
{
    public const int MaximumLength = 50_000;

    [ProtoMember(1)] public string Markdown { get; set; } = "";
    [ProtoMember(2)] public List<MediaRef> Media { get; set; } = [];

    [JsonIgnore] public bool IsEmpty => string.IsNullOrWhiteSpace(Markdown);

    public MediaRef? MediaById(string id) => Media.FirstOrDefault(media => media.Id == id);
}
