using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class CancellationUrgencyTests
{
    private static readonly DateTime Now = ChurchTime.ToUtc(new DateTime(2026, 11, 15, 7, 30, 0));

    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(true, 1, false)]
    [InlineData(false, 0, false)]
    public void TellingPeopleADateIsOffIsUrgentOnlyOnTheDay(bool notOn, int daysAway, bool urgent) =>
        Assert.Equal(urgent, EventEditingService.IsCancelledToday(new EventOccurrence { NotOn = notOn }, ChurchTime.DayOf(Now).AddDays(daysAway), Now));
}
