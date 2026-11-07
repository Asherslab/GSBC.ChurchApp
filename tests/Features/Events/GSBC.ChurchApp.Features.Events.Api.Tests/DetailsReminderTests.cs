using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class DetailsReminderTests
{
    [Fact]
    public void OrganisersAreRemindedOnceAtSevenDaysAndOnceAtTwo()
    {
        Assert.Empty(DetailsReminders.Due([7, 2], 8, []));
        Assert.Equal([7], DetailsReminders.Due([7, 2], 7, []));
        Assert.Empty(DetailsReminders.Due([7, 2], 5, [7]));
        Assert.Equal([2], DetailsReminders.Due([7, 2], 2, [7]));
        Assert.Empty(DetailsReminders.Due([7, 2], 1, [7, 2]));
        Assert.Equal([7, 2], DetailsReminders.Due([7, 2], 1, []));
        Assert.Equal([7, 2], DetailsReminders.Due([], 1, []));
    }

    [Fact]
    public void AReminderSettlesWhenTheDetailsArriveTheDateIsNotOnOrItHasGone()
    {
        Assert.False(DetailsReminders.Settles(new EventOccurrence { Missing = [EventFieldKeys.Place] }));
        Assert.True(DetailsReminders.Settles(new EventOccurrence()));
        Assert.True(DetailsReminders.Settles(new EventOccurrence { Missing = [EventFieldKeys.Place], NotOn = true }));
        Assert.True(DetailsReminders.Settles(null));
    }

    [Fact]
    public void TheReminderSaysWhichDateAndWhatIsMissing() =>
        Assert.Equal("Outreach Saturday on Saturday 14 November needs a place and a contact",
            DetailsReminders.Title(new EventOccurrence
            {
                Fields = new EventFields { Title = "Outreach Saturday" }, Missing = [EventFieldKeys.Place, EventFieldKeys.Contacts],
            }, new DateOnly(2026, 11, 14)));
}
