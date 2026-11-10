using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class CalendarChoices
{
    public static bool IsGiven(CalendarInfo calendar) =>
        Audience.TryParse(calendar.Audience, out Audience? audience) && audience.IsForSomeone;

    public static bool InYourWeek(CalendarInfo calendar, MyCalendars mine) =>
        mine.Added.Contains(calendar.Id)
        || (!mine.TurnedOff.Contains(calendar.Id) && (calendar.ShownOnNewPhones || IsGiven(calendar)));
}
