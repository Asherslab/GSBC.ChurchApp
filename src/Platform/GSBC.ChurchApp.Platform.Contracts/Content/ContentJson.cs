using System.Text.Json;
using System.Text.Json.Serialization;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
[JsonSerializable(typeof(RichText))]
[JsonSerializable(typeof(MediaRef))]
[JsonSerializable(typeof(CustomFont))]
[JsonSerializable(typeof(PastedNode))]
internal sealed partial class ContentJsonContext : JsonSerializerContext;

public static class ContentJson
{
    public static string Write(RichText content) => JsonSerializer.Serialize(content, ContentJsonContext.Default.RichText);

    public static string Write(MediaRef media) => JsonSerializer.Serialize(media, ContentJsonContext.Default.MediaRef);

    public static string Write(CustomFont font) => JsonSerializer.Serialize(font, ContentJsonContext.Default.CustomFont);

    public static bool TryRead(string json, out PastedNode? pasted) => TryRead(json, ContentJsonContext.Default.PastedNode, out pasted);

    public static bool TryRead(string json, out CustomFont? font) => TryRead(json, ContentJsonContext.Default.CustomFont, out font);

    public static bool TryRead(string json, out RichText? content) => TryRead(json, ContentJsonContext.Default.RichText, out content);

    public static bool TryRead(string json, out MediaRef? media) => TryRead(json, ContentJsonContext.Default.MediaRef, out media);

    private static bool TryRead<T>(string json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type, out T? value) where T : class
    {
        try
        {
            value = JsonSerializer.Deserialize(json, type);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
