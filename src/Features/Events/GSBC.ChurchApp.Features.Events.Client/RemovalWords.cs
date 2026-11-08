using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class RemovalWords
{
    public static string Title(CalendarRemoval removal) => $"Remove {removal.Name}?";

    public static IReadOnlyList<string> Lines(CalendarRemoval removal)
    {
        List<string> lines = [WhatGoes(removal)];
        if (removal.ShownElsewhere > 0)
            lines.Add(removal.ShownElsewhere == 1
                ? "One of them is also shown on another calendar, and comes off it too."
                : $"{removal.ShownElsewhere} of them are also shown on other calendars, and come off those too.");
        if (removal.FromElsewhere > 0)
            lines.Add(removal.FromElsewhere == 1
                ? "One event from another calendar is shown here. It stays on its own calendar."
                : $"{removal.FromElsewhere} events from other calendars are shown here. They stay on their own calendars.");
        lines.Add(WhoHasIt(removal));
        lines.Add("This can't be undone.");
        return lines;
    }

    public static string Confirm(CalendarRemoval removal) =>
        removal.Events + removal.RegularEvents == 0 ? $"Remove {removal.Name}" : $"Remove {removal.Name} and its events";

    private static string WhatGoes(CalendarRemoval removal) =>
        Capitalised((removal.Events, removal.RegularEvents) switch
        {
            (0, 0) => "It has no events.",
            (_, 0) => $"{Count(removal.Events, "event")} in it go{(removal.Events == 1 ? "es" : "")} too.",
            (0, _) => $"{Count(removal.RegularEvents, "regular event")} in it, with every date, go{(removal.RegularEvents == 1 ? "es" : "")} too.",
            _ => $"{Count(removal.Events, "event")} and {Count(removal.RegularEvents, "regular event")} (with every date) in it go too.",
        });

    private static string WhoHasIt(CalendarRemoval removal)
    {
        string added = removal.AddedBy switch
        {
            0 => "Nobody has added it",
            1 => "One person or phone has added it",
            _ => $"{removal.AddedBy} people and phones have added it",
        };
        string defaults = removal.ShownOnNewPhones ? ", and it's shown on every new phone" : "";
        string apps = removal.ForEveryone ? " Calendar apps subscribed to its link stop getting it." : "";
        return $"{added}{defaults}. It comes off every phone that has it the next time it connects.{apps}";
    }

    private static string Count(int count, string thing) => count == 1 ? $"one {thing}" : $"{count} {thing}s";

    private static string Capitalised(string sentence) => string.Concat(sentence[..1].ToUpperInvariant(), sentence[1..]);
}
