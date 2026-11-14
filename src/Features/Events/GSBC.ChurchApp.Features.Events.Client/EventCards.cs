using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Content;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Events.Client;

public interface IEventCards
{
    IReadOnlyList<string> Feeds { get; }

    Task<Diary> DiaryAsync();
}

internal sealed class DiaryEventCards(CalendarDiary diary) : IEventCards
{
    public IReadOnlyList<string> Feeds => CalendarDiary.Feeds;

    public Task<Diary> DiaryAsync() => diary.LoadAsync();
}

public sealed record EventCardShown(EventOccurrence Occurrence, CalendarInfo? Calendar, bool AsPattern);

public static class EventCardFacts
{
    private const int MostChoices = 40;

    public static EventCardShown? Find(Diary diary, AppCard card, DateTime nowUtc)
    {
        if (card.Shows == EventShows.OneDate)
            return diary.Find(card.Id) is { } occurrence ? Shown(diary, occurrence, false) : null;
        if (!Guid.TryParseExact(card.Id, "N", out Guid itemId))
            return null;
        List<EventOccurrence> dates = diary.Occurrences.Where(occurrence => ItemOf(occurrence) == itemId).OrderBy(occurrence => occurrence.StartsAt).ToList();
        IEnumerable<EventOccurrence> candidates = card.Shows == EventShows.Pattern ? dates.Where(occurrence => !occurrence.NotOn) : dates;
        return candidates.FirstOrDefault(occurrence => occurrence.EndsAt >= nowUtc) is { } next ? Shown(diary, next, card.Shows == EventShows.Pattern) : null;
    }

    public static DateOnly? DayOf(AppCard card) =>
        card.Shows == EventShows.OneDate && EventDates.TryReadOccurrenceId(card.Id, out _, out DateOnly? date) ? date : null;

    public static bool IsOver(AppCard card, DateTime nowUtc) => DayOf(card) is { } day && day < ChurchTime.DayOf(nowUtc);

    public static IReadOnlyList<AppCardChoice> Choices(Diary diary, string text, DateTime nowUtc) =>
        diary.Occurrences
            .Where(occurrence => occurrence.EndsAt >= nowUtc && !occurrence.NotOn)
            .Where(occurrence => text.Length == 0 || occurrence.Fields.Title.Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(occurrence => occurrence.StartsAt)
            .DistinctBy(occurrence => occurrence.Kind == OccurrenceKind.OneOff ? occurrence.Id : occurrence.ItemId)
            .Take(MostChoices)
            .Select(Choice)
            .ToList();

    public static bool IsForEveryone(EventCardShown shown) => shown.Calendar is null || shown.Calendar.Audience.Length == 0 || shown.Calendar.Audience == Audience.Everyone.Text;

    private static AppCardChoice Choice(EventOccurrence occurrence)
    {
        string when = EventWords.Join(EventWords.LongDate(EventWords.DayOf(occurrence)), EventWords.Times(occurrence));
        bool regular = occurrence.Kind != OccurrenceKind.OneOff && ItemOf(occurrence) is not null;
        return new AppCardChoice(new AppCard(AppCardKind.Event, occurrence.Id), occurrence.Fields.Title, when, occurrence.Fields.Picture,
            regular ? occurrence.Repeats : null, regular ? ItemOf(occurrence)!.Value.ToString("N") : null);
    }

    private static Guid? ItemOf(EventOccurrence occurrence) => Guid.TryParse(occurrence.ItemId, out Guid id) ? id : null;

    private static EventCardShown Shown(Diary diary, EventOccurrence occurrence, bool pattern) => new(occurrence, diary.Calendar(occurrence.CalendarId), pattern);
}

internal sealed class EventCardChoices(IEventCards cards, TimeProvider time) : IAppCardSource
{
    public IReadOnlyList<AppCardKind> Kinds => [AppCardKind.Event];

    public IReadOnlyList<string> Feeds => cards.Feeds;

    public async Task<IReadOnlyList<AppCardChoice>> ChoicesAsync(AppCardKind kind, string text) =>
        EventCardFacts.Choices(await cards.DiaryAsync(), text.Trim(), time.GetUtcNow().UtcDateTime);
}
