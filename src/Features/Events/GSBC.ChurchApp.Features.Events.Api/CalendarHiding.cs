using GSBC.ChurchApp.Features.Events.Api.Data;

namespace GSBC.ChurchApp.Features.Events.Api;

internal static class CalendarHiding
{
    public static bool IsHidden(CalendarRecord calendar) => calendar.ShownAudience is not null;

    public static string Chosen(CalendarRecord calendar) => calendar.ShownAudience ?? calendar.Audience;

    public static void Hide(CalendarRecord calendar, string whileHidden)
    {
        calendar.ShownAudience ??= calendar.Audience;
        calendar.Audience = whileHidden;
    }

    public static void Show(CalendarRecord calendar)
    {
        if (calendar.ShownAudience is not { } shown)
            return;
        (calendar.Audience, calendar.ShownAudience) = (shown, null);
    }

    public static void Choose(CalendarRecord calendar, string audience)
    {
        if (!IsHidden(calendar))
            calendar.Audience = audience;
        else if (audience != calendar.Audience)
            calendar.ShownAudience = audience;
    }
}
