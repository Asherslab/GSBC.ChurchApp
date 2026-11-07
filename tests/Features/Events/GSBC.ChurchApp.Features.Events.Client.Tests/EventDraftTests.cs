using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client.Tests;

public sealed class EventDraftTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Anniversary = new(2026, 11, 15);

    [Fact]
    public void ThisDateOnlyStartsFromTheRegularEventWithOnlyWhatTheSpecialChanged()
    {
        RegularEventDetail sundays = new()
        {
            Id = Guid.NewGuid().ToString(), CalendarId = Guid.NewGuid().ToString(),
            Fields = new EventFields { Title = "Morning service", Place = new EventPlace { Name = "Auditorium" } },
            Pattern = new RegularPattern { Repeats = Repeats.Weekly, Days = [DayOfWeek.Sunday], StartDate = "2026-01-04" },
            StartMinute = 630, EndMinute = 720,
            Specials =
            [
                new SpecialInfo
                {
                    Date = EventDates.Format(Anniversary), Changed = [EventFieldKeys.Title, EventFieldKeys.Time],
                    Fields = new EventFields { Title = "Anniversary Sunday", Place = new EventPlace { Name = "Stale copy" } },
                    StartMinute = 540, EndMinute = 720,
                },
            ],
        };

        EventDraft draft = EventDraft.From(sundays, Anniversary, Now);

        Assert.Equal(DraftScope.ThisDate, draft.Scope);
        Assert.Equal("Anniversary Sunday", draft.ThisDate.Fields.Title);
        Assert.Equal("Auditorium", draft.ThisDate.Fields.Place?.Name);
        Assert.Equal(540, draft.ThisDate.StartMinute);
        Assert.Equal("Morning service", draft.Every.Fields.Title);
        Assert.Equal(630, draft.Every.StartMinute);
    }

    [Fact]
    public void EditingThisDateNeverChangesWhatEveryDateWouldSave()
    {
        RegularEventDetail sundays = new()
        {
            Id = Guid.NewGuid().ToString(), CalendarId = Guid.NewGuid().ToString(),
            Fields = new EventFields { Title = "Evening service" },
            Pattern = new RegularPattern { Repeats = Repeats.Weekly, Days = [DayOfWeek.Sunday], StartDate = "2026-01-04" },
            StartMinute = 1080,
        };
        EventDraft draft = EventDraft.From(sundays, Anniversary, Now);

        draft.Fields.Title = "Carols by candlelight";

        Assert.Equal("Carols by candlelight", draft.ToDateSave(notOn: false).Fields.Title);
        Assert.Equal("Evening service", draft.ToRegularSave(keepOwnConfirmed: false).Fields.Title);
    }
}
