using System.Globalization;
using System.Text.Json;

namespace GSBC.ChurchApp.Features.People.Api.SafeMinistry;

internal sealed class SmcRow(IReadOnlyDictionary<string, string> fields)
{
    public string this[string name] => fields.GetValueOrDefault(name, "");

    public string Id => this["smc_id"];

    public string Name => string.Join(' ', new[] { this["firstname"], this["surname"] }.Where(part => part.Length > 0));

    public DateOnly? Day(string name) =>
        this[name] is { Length: >= 10 } text
        && DateOnly.TryParseExact(text[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day)
        && day.Year >= 1900
            ? day
            : null;

    public static SmcRow From(JsonElement person) =>
        new(person.EnumerateObject().GroupBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(field => field.Key, field => Text(field.First().Value), StringComparer.OrdinalIgnoreCase));

    private static string Text(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!.Trim(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "1",
        JsonValueKind.False => "0",
        _ => "",
    };
}

internal sealed record SmcPage(IReadOnlyList<SmcRow> People, int Pages);

internal static class SmcJson
{
    public static SmcPage Page(string json)
    {
        using JsonDocument document = Read(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("people", out JsonElement people) || people.ValueKind != JsonValueKind.Array)
            throw Unreadable();

        int pages = root.TryGetProperty("pagination", out JsonElement pagination) && pagination.ValueKind == JsonValueKind.Object
            && pagination.TryGetProperty("pages", out JsonElement count) && count.ValueKind == JsonValueKind.Number && count.TryGetInt32(out int number)
                ? number
                : 1;
        return new SmcPage(people.EnumerateArray().Where(person => person.ValueKind == JsonValueKind.Object).Select(SmcRow.From).ToList(), pages);
    }

    public static SmcRow Person(string json)
    {
        using JsonDocument document = Read(json);
        return document.RootElement.ValueKind == JsonValueKind.Object ? SmcRow.From(document.RootElement) : throw Unreadable();
    }

    private static JsonDocument Read(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw Unreadable();
        }
    }

    private static SmcException Unreadable() => new("Safe Ministry Check sent back something the app couldn't read.");
}

internal sealed class SmcException(string message) : Exception(message);
