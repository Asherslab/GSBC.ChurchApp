using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class ExpansionTests
{
    private static readonly DateOnly November = new(2026, 11, 1);

    [Fact]
    public void EachPatternPutsItsEventOnTheRightDates()
    {
        Assert.Equal(["2026-11-04", "2026-11-06", "2026-11-11", "2026-11-13"],
            Dates(new RegularPattern { Repeats = Repeats.Weekly, Days = [DayOfWeek.Wednesday, DayOfWeek.Friday] }, November, November.AddDays(13)));
        Assert.Equal(["2026-11-14", "2026-11-28", "2026-12-12"],
            Dates(new RegularPattern { Repeats = Repeats.EverySecondWeek, Days = [DayOfWeek.Saturday], StartDate = "2026-11-14" }, November, new DateOnly(2026, 12, 20)));
        Assert.Equal(["2026-11-01", "2026-12-06", "2027-01-03"],
            Dates(new RegularPattern { Repeats = Repeats.MonthlyByWeekday, Days = [DayOfWeek.Sunday], WeekOfMonth = 1 }, November, new DateOnly(2027, 1, 31)));
        Assert.Equal(["2026-11-27", "2026-12-25"],
            Dates(new RegularPattern { Repeats = Repeats.MonthlyByWeekday, Days = [DayOfWeek.Friday], WeekOfMonth = -1 }, November, new DateOnly(2026, 12, 31)));
        Assert.Equal(["2026-12-31", "2027-01-31", "2027-03-31"],
            Dates(new RegularPattern { Repeats = Repeats.MonthlyByDate, DayOfMonth = 31 }, November, new DateOnly(2027, 4, 15)));
        Assert.Equal(["2026-11-03", "2026-11-17"],
            Dates(new RegularPattern { Repeats = Repeats.OnDates, Dates = ["2026-11-17", "2026-11-03", "2027-02-01"] }, November, new DateOnly(2026, 12, 31)));
        Assert.Equal(["2026-11-08", "2026-11-15"],
            Dates(new RegularPattern { Repeats = Repeats.Weekly, Days = [DayOfWeek.Sunday], StartDate = "2026-11-08", EndDate = "2026-11-20" }, November, new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void ABreakShowsItsFirstDatesAsNotOnWithItsNameAndWhenItsBackThenLeavesTheRestOut()
    {
        RegularEventRecord evening = Regular("Evening service", Weekly(DayOfWeek.Sunday));
        BreakRecord christmas = new() { Name = "Christmas break", StartDate = new DateOnly(2026, 12, 21), EndDate = new DateOnly(2027, 1, 25) };

        List<EventOccurrence> dates = Expansion.OccurrencesOf(evening, new Dictionary<DateOnly, SpecialRecord>(), [christmas],
            new DateOnly(2026, 12, 20), new DateOnly(2027, 2, 7)).ToList();

        Assert.Equal(["2026-12-20", "2026-12-27", "2027-01-03", "2027-01-31", "2027-02-07"], dates.Select(date => date.Date));
        Assert.All(dates.Where(date => date.Date is "2026-12-27" or "2027-01-03"), date =>
            Assert.Equal((true, "Christmas break", "2027-01-31", "Evening service"), (date.NotOn, date.NotOnReason, date.BackOn, date.Fields.Title)));
        Assert.False(dates[0].NotOn);
    }

    [Fact]
    public void ASpecialOverridesOnlyWhatItChangedAndKeepsInStepWithTheRest()
    {
        RegularEventRecord morning = Regular("Morning service", Weekly(DayOfWeek.Sunday));
        morning.Fields.Place = new EventPlace { Name = "Auditorium" };
        SpecialRecord anniversary = new()
        {
            RegularEventId = morning.Id, Date = new DateOnly(2026, 11, 15), Changed = [EventFieldKeys.Title, EventFieldKeys.Time],
            Fields = new EventFields { Title = "Anniversary Sunday Service", Place = new EventPlace { Name = "Typed but not changed" } },
            StartMinute = 9 * 60,
        };
        morning.Fields.Place = new EventPlace { Name = "Fellowship Area" };
        morning.StartMinute = 10 * 60 + 30;

        EventOccurrence special = Expansion.OccurrencesOf(morning, new Dictionary<DateOnly, SpecialRecord> { [anniversary.Date] = anniversary }, [],
            anniversary.Date, anniversary.Date).Single();

        Assert.Equal(OccurrenceKind.Special, special.Kind);
        Assert.Equal("Anniversary Sunday Service", special.Fields.Title);
        Assert.Equal("Fellowship Area", special.Fields.Place!.Name);
        Assert.Equal(9, Platform.Common.ChurchTime.ToChurch(special.StartsAt).Hour);
    }

    private static RegularPattern Weekly(DayOfWeek day) => new() { Repeats = Repeats.Weekly, Days = [day], StartDate = "2026-01-04" };

    private static RegularEventRecord Regular(string title, RegularPattern pattern) => new()
    {
        Id = Guid.CreateVersion7(), Fields = new EventFields { Title = title }, Pattern = pattern, StartMinute = 18 * 60, EndMinute = 19 * 60 + 30,
    };

    private static List<string> Dates(RegularPattern pattern, DateOnly from, DateOnly to) =>
        Expansion.DatesOf(pattern, from, to).Select(EventDates.Format).ToList();
}
