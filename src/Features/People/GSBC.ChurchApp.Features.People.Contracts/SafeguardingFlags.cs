namespace GSBC.ChurchApp.Features.People.Contracts;

public static class FlagKinds
{
    public const string RunOut = "run-out";
    public const string DueSoon = "due-soon";
    public const string NotRecorded = "not-recorded";
    public const string NoParent = "no-parent";
}

public enum FlagSubject
{
    BlueCard,
    Training,
    Family,
}

public sealed record SafeguardingFlag(FlagSubject Subject, string Kind, string Words, DateOnly? When)
{
    public bool RunOut => Kind == FlagKinds.RunOut;
}

public static class SafeguardingFlags
{
    public const int SoonDays = 60;

    public static IReadOnlyList<SafeguardingFlag> For(SafeguardingRecord? record, DateOnly today)
    {
        if (record is null || (!record.HasCard && !record.HasTraining))
            return [];

        List<SafeguardingFlag> flags = [];
        if (!record.HasCard)
            flags.Add(new SafeguardingFlag(FlagSubject.BlueCard, FlagKinds.NotRecorded, "No blue card recorded", null));
        else if (record.CardStatus.Length > 0 && !BlueCards.Holds(record.CardStatus))
            flags.Add(new SafeguardingFlag(FlagSubject.BlueCard, FlagKinds.RunOut, $"Blue card: {BlueCards.Words(record.CardStatus).ToLowerInvariant()}", WireDates.FromWire(record.CardExpires)));
        else if (Dated(FlagSubject.BlueCard, "Blue card", "expired", "expires", WireDates.FromWire(record.CardExpires), today) is { } card)
            flags.Add(card);

        if (!record.HasTraining)
            flags.Add(new SafeguardingFlag(FlagSubject.Training, FlagKinds.NotRecorded, "No Safe Ministry training recorded", null));
        else if (Dated(FlagSubject.Training, "Safe Ministry training", "was due", "due", WireDates.FromWire(record.TrainingDue), today) is { } training)
            flags.Add(training);

        return flags;
    }

    public static SafeguardingFlag NoParent(DateOnly? askedOn) =>
        new(FlagSubject.Family, FlagKinds.NoParent, askedOn is null ? "Under 18, no parent linked" : "Under 18, waiting for a parent to answer", askedOn);

    public static string DaysWords(DateOnly day, DateOnly today) => (day.DayNumber - today.DayNumber) switch
    {
        0 => "today",
        1 => "tomorrow",
        -1 => "yesterday",
        > 0 and var days => $"in {days} days",
        var days => $"{-days} days ago",
    };

    private static SafeguardingFlag? Dated(FlagSubject subject, string what, string past, string future, DateOnly? day, DateOnly today) => day switch
    {
        null => null,
        { } date when date < today => new SafeguardingFlag(subject, FlagKinds.RunOut, $"{what} {past} {date:d MMM yyyy}", date),
        { } date when date.DayNumber - today.DayNumber <= SoonDays => new SafeguardingFlag(subject, FlagKinds.DueSoon, $"{what} {future} {date:d MMM yyyy}", date),
        _ => null,
    };
}
