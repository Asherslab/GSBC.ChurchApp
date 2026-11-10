using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class KeptRegularEventTests
{
    private static readonly TimeOnly Nine = new(9, 0), QuarterToTen = new(9, 45), Ten = new(10, 0);

    [Fact]
    public void ItsDatesAreOnAtTheirOwnTimeAndItsGapsShowAsNotOnLikeABreak()
    {
        KeptRegularEvent wanted = Kept(
            [new(Sunday(1), Nine, QuarterToTen), new(Sunday(2), Nine, QuarterToTen), new(Sunday(5), Nine, QuarterToTen), new(Sunday(6), Ten, new TimeOnly(11, 0))],
            [new(Sunday(3), "Between groups"), new(Sunday(4), "Between groups")]);

        List<EventOccurrence> dates = Occurrences(wanted, Sunday(0), Sunday(10));

        Assert.Equal([Sunday(1), Sunday(2), Sunday(3), Sunday(4), Sunday(5), Sunday(6)], dates.Select(date => EventDates.Parse(date.Date)!.Value));
        Assert.All(dates.Where(date => !date.NotOn), date => Assert.Equal(("Life Groups", "/life-groups"), (date.KeptBy, date.OpensAt)));
        Assert.Equal((9 * 60, 9 * 60 + 45), Minutes(dates[0]));
        Assert.Equal((10 * 60, 11 * 60), Minutes(dates[5]));
        Assert.All(dates, date => Assert.Equal(OccurrenceKind.Regular, date.Kind));
        Assert.All(dates.Skip(2).Take(2), gap => Assert.Equal((true, "Between groups", EventDates.Format(Sunday(5))), (gap.NotOn, gap.NotOnReason, gap.BackOn)));
    }

    [Fact]
    public void WithNothingPlannedItHasNoDates()
    {
        Assert.Empty(Occurrences(Kept([], []), Sunday(0), Sunday(10)));
    }

    private static KeptRegularEvent Kept(IReadOnlyList<KeptDate> on, IReadOnlyList<KeptGap> notOn) =>
        new("tests.kept", "Life Groups", "/life-groups", "Life Groups", "", on, notOn);

    private static List<EventOccurrence> Occurrences(KeptRegularEvent wanted, DateOnly from, DateOnly to)
    {
        RegularEventRecord regular = new() { Id = Guid.CreateVersion7(), Keeper = wanted.Keeper, KeptBy = wanted.KeptBy, OpensAt = wanted.OpensAt, Fields = new EventFields { Title = wanted.Title } };
        KeptShape shape = KeptShape.Of(regular.Id, wanted, TestClock.Start);
        (regular.Pattern, regular.StartMinute, regular.EndMinute) = (shape.Pattern, shape.StartMinute, shape.EndMinute);
        return Expansion.OccurrencesOf(regular, shape.Specials.ToDictionary(special => special.Date), shape.Breaks, from, to).ToList();
    }

    private static (int?, int?) Minutes(EventOccurrence occurrence)
    {
        DateTime starts = Platform.Common.ChurchTime.ToChurch(occurrence.StartsAt), ends = Platform.Common.ChurchTime.ToChurch(occurrence.EndsAt);
        return (starts.Hour * 60 + starts.Minute, ends.Hour * 60 + ends.Minute);
    }

    private static DateOnly Sunday(int week) => new DateOnly(2026, 11, 1).AddDays(7 * week);
}
