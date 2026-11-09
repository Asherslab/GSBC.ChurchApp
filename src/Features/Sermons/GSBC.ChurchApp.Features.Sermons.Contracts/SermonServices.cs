namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonServices
{
    private const string SundayLifeGroups = "Sunday Life Groups";
    public const string SundayMorning = "Sunday Morning";
    public const string SundayEvening = "Sunday Evening";
    private const string WednesdayNight = "Wednesday Night";
    public const int NameLimit = 80;

    public static IReadOnlyList<string> Standard { get; } = [SundayLifeGroups, SundayMorning, SundayEvening, WednesdayNight];

    private static readonly Dictionary<string, string> StandardSpellings = new(StringComparer.Ordinal)
    {
        ["sunday life groups"] = SundayLifeGroups, ["sunday life group"] = SundayLifeGroups, ["life groups"] = SundayLifeGroups,
        ["sunday morning"] = SundayMorning, ["sunday am"] = SundayMorning, ["sun am"] = SundayMorning,
        ["sunday evening"] = SundayEvening, ["sunday night"] = SundayEvening, ["sunday pm"] = SundayEvening, ["sun pm"] = SundayEvening,
        ["wednesday night"] = WednesdayNight, ["wednesday evening"] = WednesdayNight, ["wednesday pm"] = WednesdayNight,
        ["wednesday"] = WednesdayNight, ["wed pm"] = WednesdayNight, ["wed"] = WednesdayNight,
    };

    private static string Key(string service) => string.Join(' ', service.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    public static string? StandardFor(string service) => StandardSpellings.GetValueOrDefault(Key(service));

    public static bool IsStandard(string service) => Standard.Contains(service);

    public static string Named(string service, IEnumerable<string> known)
    {
        string tidy = string.Join(' ', service.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (tidy.Length == 0)
            return "";
        return StandardFor(tidy) ?? known.FirstOrDefault(existing => Key(existing) == Key(tidy)) ?? tidy;
    }
}
