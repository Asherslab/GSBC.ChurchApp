using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public sealed class CalendarHidingTests
{
    private static readonly string Families = Audience.HoldingRole(Guid.CreateVersion7()).Text;
    private static readonly string Team = Audience.HoldingRole(Guid.CreateVersion7()).Text;
    private static readonly string LeadersOnly = Audience.HoldingRole(Guid.CreateVersion7()).Text;

    [Fact]
    public void AHiddenCalendarReachesOnlyItsLeadersAndGetsBackTheAudienceItWasLastGiven()
    {
        CalendarRecord calendar = new() { Name = "Impact Kids", Slug = "impact-kids", Audience = Families };

        CalendarHiding.Hide(calendar, LeadersOnly);
        CalendarHiding.Hide(calendar, LeadersOnly);
        Assert.Equal((LeadersOnly, Families), (calendar.Audience, CalendarHiding.Chosen(calendar)));

        CalendarHiding.Choose(calendar, Team);
        Assert.Equal((LeadersOnly, Team), (calendar.Audience, CalendarHiding.Chosen(calendar)));

        CalendarHiding.Show(calendar);
        Assert.Equal(Team, calendar.Audience);
        Assert.False(CalendarHiding.IsHidden(calendar));
    }
}
