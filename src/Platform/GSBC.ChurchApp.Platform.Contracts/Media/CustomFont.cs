using System.Text.Json.Serialization;
using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Media;

[ProtoContract]
public sealed class CustomFont
{
    [ProtoMember(1)] public string Family { get; set; } = "";
    [ProtoMember(2)] public List<FontFile> Files { get; set; } = [];

    [JsonIgnore] public string CssFamily => $"gsbc-font-{Fingerprint}";

    private string Fingerprint
    {
        get
        {
            string name = Files.Count == 0 ? "none" : Path.GetFileNameWithoutExtension(Files[0].Media.Key);
            return new string(name.Where(char.IsAsciiLetterOrDigit).Take(16).ToArray());
        }
    }

    [JsonIgnore] public bool HasBold => Files.Any(file => file.Weight.Contains("700", StringComparison.Ordinal));
}

[ProtoContract]
public sealed class FontFile
{
    public const string Regular = "400";
    public const string Bold = "700";
    public const string RegularToBold = "400 700";

    [ProtoMember(1, IsRequired = true)] public MediaRef Media { get; set; } = new();
    [ProtoMember(2, IsRequired = true)] public string Weight { get; set; } = Regular;
}

[ProtoContract]
public sealed class FontFamilyChoice
{
    [ProtoMember(1)] public string Family { get; set; } = "";
    [ProtoMember(2)] public string Category { get; set; } = "";
}
