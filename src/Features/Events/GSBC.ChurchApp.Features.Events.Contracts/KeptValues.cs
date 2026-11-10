namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class KeptValues
{
    public static Timed Effective(Timed every, SpecialInfo special)
    {
        bool ownTime = special.Changed.Contains(EventFieldKeys.Time);
        return new Timed(FieldValues.Overlay(every.Fields, special.Fields, special.Changed),
            ownTime ? special.StartMinute : every.StartMinute, ownTime ? special.EndMinute : every.EndMinute);
    }

    public static List<KeptValue> Of(Timed every, IReadOnlyCollection<string> changed, IEnumerable<SpecialInfo> upcoming) =>
        upcoming.SelectMany(special => changed.Where(special.Changed.Contains).Select(field => new KeptValue
        {
            Date = special.Date,
            Title = Effective(every, special).Fields.Title,
            Field = field,
            Value = FieldValues.Words(Effective(every, special), field),
        })).ToList();

    public static List<KeptValue> Of(RegularEventDetail before, RegularEventSave after, DateOnly today)
    {
        Timed every = new(before.Fields, before.StartMinute, before.EndMinute);
        List<string> changed = FieldValues.Differences(every, new Timed(after.Fields, after.StartMinute, after.EndMinute));
        return changed.Count == 0 ? [] : Of(every, changed, before.Specials.Where(special => EventDates.Parse(special.Date) >= today).OrderBy(special => special.Date, StringComparer.Ordinal));
    }
}
