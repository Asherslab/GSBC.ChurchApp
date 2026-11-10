using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EditingOffers
{
    private const int MostPlacesUsed = 50;
    private const int MostOffered = 50;

    public static IReadOnlyList<EventPlace> PlacesUsed(EditingBook book) =>
        book.Events.Values.Select(detail => (At: detail.UpdatedAt, detail.Fields.Place))
            .Concat(book.Regulars.Values.SelectMany(regular => regular.Detail.Specials
                .Select(special => (At: special.ChangedAt ?? regular.Detail.UpdatedAt, special.Fields.Place))
                .Append((At: regular.Detail.UpdatedAt, regular.Detail.Fields.Place))))
            .Where(used => used.Place is { PlaceId: null or "" } place && (place.Name.Length > 0 || place.Address.Length > 0))
            .OrderByDescending(used => used.At)
            .Select(used => used.Place!)
            .DistinctBy(place => (place.Name.Trim().ToLowerInvariant(), place.Address.Trim().ToLowerInvariant()))
            .Take(MostPlacesUsed)
            .ToList();

    public static IReadOnlyList<MediaRef> PicturesUsed(EditingBook book, Diary diary, string calendarId) =>
        book.Events.Values.Where(detail => detail.CalendarId == calendarId).OrderByDescending(detail => detail.UpdatedAt).Select(detail => detail.Fields.Picture)
            .Concat(book.Regulars.Values.Select(regular => regular.Detail).Where(detail => detail.CalendarId == calendarId).OrderByDescending(detail => detail.UpdatedAt)
                .SelectMany(detail => detail.Specials.Select(special => special.Fields.Picture).Prepend(detail.Fields.Picture)))
            .Concat(diary.Occurrences.OrderBy(occurrence => occurrence.CalendarId != calendarId).Select(occurrence => occurrence.Fields.Picture))
            .OfType<MediaRef>()
            .DistinctBy(picture => picture.Id)
            .ToList();

    public static ShowableList Showable(Diary diary, EditingBook book, string yours, string search, DateOnly today, Func<string, bool> edits)
    {
        Dictionary<string, Audience> audiences = diary.Calendars.ToDictionary(calendar => calendar.Id, calendar => CalendarAudiences.Of(calendar.Audience));
        foreach (EditingCalendar calendar in book.Calendars)
            audiences[calendar.Settings.Calendar.Id] = CalendarAudiences.Of(calendar.Settings.Calendar.Audience);
        Dictionary<string, CalendarInfo> calendars = diary.Calendars.ToDictionary(calendar => calendar.Id);
        foreach (EditingCalendar calendar in book.Calendars)
            calendars[calendar.Settings.Calendar.Id] = calendar.Settings.Calendar;
        HashSet<string> asked = (book.Calendar(yours)?.Asked ?? []).Select(request => request.EventId ?? request.RegularEventId ?? "").ToHashSet();

        Dictionary<string, Offer> offers = [];
        foreach (IGrouping<string, EventOccurrence> item in diary.Occurrences.Where(occurrence => !occurrence.NotOn && EventWords.DayOf(occurrence) >= today).GroupBy(occurrence => occurrence.ItemId))
        {
            EventOccurrence next = item.MinBy(occurrence => occurrence.StartsAt)!;
            bool regular = next.Kind != OccurrenceKind.OneOff;
            offers[item.Key] = new Offer(item.Key, next.CalendarId, regular, next.Fields.Title, EventWords.DayOf(next),
                EventWords.Join(regular ? next.Repeats : EventWords.LongDate(EventWords.DayOf(next)), EventWords.Times(next)),
                Published: true, next.AlsoOn);
        }
        foreach (EventDetail detail in book.Events.Values.Where(detail => !detail.NotOn))
            if (EventDates.Parse(detail.Times.StartDate) is { } start && (EventDates.Parse(detail.Times.EndDate) ?? start) >= today)
                offers[detail.Id] = new Offer(detail.Id, detail.CalendarId, false, detail.Fields.Title, start,
                    EventWords.Join(EventWords.LongDate(start), EventDates.Times(detail.Times.StartMinute, detail.Times.EndMinute)),
                    IsShown(detail.Showing, detail.ShowFrom, today), detail.AlsoOn.Where(showing => showing.Agreed).Select(showing => showing.CalendarId).ToList());
        foreach (RegularEventDetail detail in book.Regulars.Values.Select(regular => regular.Detail))
            if (detail.Dates.FirstOrDefault(date => !date.NotOn) is { } first && EventDates.Parse(first.Date) is { } next)
                offers[detail.Id] = new Offer(detail.Id, detail.CalendarId, true, detail.Fields.Title, next,
                    EventWords.Join(detail.Repeats, EventDates.Times(detail.StartMinute, detail.EndMinute)),
                    IsShown(detail.Showing, detail.ShowFrom, today), detail.AlsoOn.Where(showing => showing.Agreed).Select(showing => showing.CalendarId).ToList());

        string wanted = search.Trim();
        Audience yoursAudience = audiences.GetValueOrDefault(yours) ?? Audience.SignedIn;
        return new ShowableList
        {
            Items = offers.Values
                .Where(offer => offer.HomeId != yours && !offer.AlsoOn.Contains(yours) && calendars.TryGetValue(offer.HomeId, out CalendarInfo? home) && !home.ReadOnly)
                .Where(offer => wanted.Length == 0
                    || offer.Title.Contains(wanted, StringComparison.CurrentCultureIgnoreCase)
                    || calendars[offer.HomeId].Name.Contains(wanted, StringComparison.CurrentCultureIgnoreCase))
                .OrderBy(offer => offer.Next).ThenBy(offer => offer.Title, StringComparer.CurrentCulture)
                .Take(MostOffered)
                .Select(offer =>
                {
                    Audience current = CalendarAudiences.Union(offer.AlsoOn.Append(offer.HomeId).Where(audiences.ContainsKey).Select(id => audiences[id]));
                    Widening widening = CalendarAudiences.Widening(current, yoursAudience);
                    return new ShowableItem
                    {
                        HomeCalendarId = offer.HomeId,
                        HomeCalendarName = calendars[offer.HomeId].Name,
                        EventId = offer.Regular ? null : offer.Id,
                        RegularEventId = offer.Regular ? offer.Id : null,
                        Title = offer.Title,
                        When = offer.When,
                        Published = offer.Published,
                        Asked = asked.Contains(offer.Id),
                        AgreesAtOnce = edits(offer.HomeId),
                        Widens = widening.Widens,
                        NewlyReaches = widening.Words,
                        NewlyReachesRoleIds = widening.NewRoleIds.Select(role => role.ToString()).ToList(),
                    };
                })
                .ToList(),
        };
    }

    private static bool IsShown(Showing showing, DateTime? showFrom, DateOnly today) =>
        showing == Showing.Shown || (showing == Showing.ShownFrom && showFrom is { } from && ChurchTime.DayOf(from) <= today);

    private sealed record Offer(string Id, string HomeId, bool Regular, string Title, DateOnly Next, string When, bool Published, IReadOnlyList<string> AlsoOn);
}
