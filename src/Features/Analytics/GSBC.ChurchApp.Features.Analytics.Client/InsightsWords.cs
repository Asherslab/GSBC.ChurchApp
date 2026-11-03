using System.Globalization;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Analytics.Client;

public static class InsightsWords
{
    public const string PhonesNotPeople = "Every number counts phones and tablets, not people: one person with an iPad counts twice, and a couple sharing a phone counts once. Counts below 5 show as “fewer than 5”.";

    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    public static string Number(Tally? tally) => tally switch
    {
        null => "0",
        { FewerThanFive: true } => "fewer than 5",
        _ => tally.Value.ToString("N0", English),
    };

    public static string Headline(Tally? tally) => tally is { FewerThanFive: true } ? "Fewer than 5" : Number(tally);

    public static bool IsPrivate(Tally? tally) => tally is { FewerThanFive: true };

    public static string? Change(Tally? now, Tally? before, string period)
    {
        if (now is null || before is null || now.FewerThanFive || before.FewerThanFive || before.Value == 0)
            return null;
        double change = (now.Value - before.Value) * 100.0 / before.Value;
        int rounded = (int)Math.Round(Math.Abs(change));
        return rounded == 0 ? $"About the same as {period}" : $"{rounded}% {(change > 0 ? "more" : "fewer")} than {period}";
    }

    public static string Group(UsageGroup group) => group switch
    {
        UsageGroup.NotSignedIn => "not signed in",
        UsageGroup.SignedIn => "signed in",
        UsageGroup.OnATeam => "on a team",
        _ => "everyone",
    };

    public static string WeekOf(int weekStart, bool withYear = false) =>
        DateOnly.FromDayNumber(weekStart).ToString(withYear ? "d MMM yyyy" : "d MMM", English);

    public static TrendPoint Point(WeekPoint week, bool withYear = false) =>
        new(WeekOf(week.WeekStart, withYear), week.Phones is { FewerThanFive: false, Value: > 0 } phones ? phones.Value : null, Number(week.Phones));
}
