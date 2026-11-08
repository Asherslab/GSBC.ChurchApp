using System.Globalization;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed record ShowableEvent(IShowable Item, CalendarRecord Home, DateOnly Next, bool Asked);

internal static class ShowableEvents
{
    private const int MostOffered = 50;

    public static IReadOnlyList<ShowableEvent> Offer(EventsBook book, Guid yours, Func<CalendarRecord, bool> canSee, DateOnly today, string search)
    {
        string wanted = search.Trim();
        return book.Events.Where(item => !item.NotOn).Select(item => (Item: (IShowable)item, Next: (item.EndDate ?? item.StartDate) >= today ? item.StartDate : (DateOnly?)null))
            .Concat(book.RegularEvents.Select(item => (Item: (IShowable)item, Next: Expansion.DatesOf(item.Pattern, today, today.AddMonths(EventsFeeds.MonthsAhead)).Cast<DateOnly?>().FirstOrDefault())))
            .Where(entry => entry.Next is not null && entry.Item.CalendarId != yours)
            .Where(entry => book.Calendars.TryGetValue(entry.Item.CalendarId, out CalendarRecord? home) && home.ICalSource is not { Length: > 0 } && canSee(home))
            .Select(entry => (entry.Item, entry.Next, Showing: book.ShowingsOf(entry.Item.Id).FirstOrDefault(showing => showing.CalendarId == yours)))
            .Where(entry => entry.Showing?.AgreedAt is null)
            .Select(entry => new ShowableEvent(entry.Item, book.Calendars[entry.Item.CalendarId], entry.Next!.Value, entry.Showing is not null))
            .Where(offered => wanted.Length == 0
                || offered.Item.Fields.Title.Contains(wanted, StringComparison.CurrentCultureIgnoreCase)
                || offered.Home.Name.Contains(wanted, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(offered => offered.Next).ThenBy(offered => offered.Item.Fields.Title)
            .Take(MostOffered)
            .ToList();
    }

    public static string When(IShowable item, DateOnly next) => item switch
    {
        RegularEventRecord regular => Join(RepeatWords.Of(regular.Pattern), EventDates.Times(regular.StartMinute, regular.EndMinute)),
        EventRecord one => Join(next.ToString("dddd d MMMM", CultureInfo.InvariantCulture), EventDates.Times(one.StartMinute, one.EndMinute)),
        _ => "",
    };

    private static string Join(string first, string second) => second.Length == 0 ? first : $"{first} · {second}";
}
