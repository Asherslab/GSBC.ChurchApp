using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class MinistryCalendarPlanTests
{
    private static readonly Guid Kids = Guid.CreateVersion7();

    private static Audience For(string audience) => MinistryViews.CalendarAudience(audience, Kids, false);

    private static MinistryCalendar Calendar(string name, Audience audience) => new(Guid.CreateVersion7(), Kids, name, name.ToLowerInvariant(), audience, false);

    [Fact]
    public void ExtraCalendarsMergeIntoOnePublicAndOneTeamCalendarNamedForTheMinistry()
    {
        MinistryCalendar camps = Calendar("Kids camps", For(Audiences.Everyone));
        MinistryCalendar kids = Calendar("Impact Kids", For(Audiences.Everyone));
        MinistryCalendar families = Calendar("Impact Kids families", For(Audiences.Attendees));
        MinistryCalendar parents = Calendar("Impact Kids parents", For(Audiences.Parents));
        MinistryCalendar helpers = Calendar("Helpers", For(Audiences.Team));
        MinistryCalendar setUp = Calendar("Set-up roster", For(Audiences.Team));

        MinistryCalendarShape shape = MinistryCalendarPlan.Shape(Kids, "Impact Kids", false, [camps, kids, families, parents, helpers, setUp]);

        Assert.Equal(
            [new CalendarMerge(kids.Id, camps.Id), new CalendarMerge(families.Id, camps.Id), new CalendarMerge(parents.Id, camps.Id), new CalendarMerge(setUp.Id, helpers.Id)],
            shape.Merges);
        Assert.Equal([new CalendarFix(camps.Id, "Impact Kids", For(Audiences.Everyone)), new CalendarFix(helpers.Id, "The Impact Kids Team", For(Audiences.Team))], shape.Fixes);
    }

    [Fact]
    public void AnAttendeesOnlyCalendarBecomesThePublicCalendarForEveryoneWhoSeesTheMinistry()
    {
        MinistryCalendar families = Calendar("Impact Kids families", For(Audiences.Attendees));

        Assert.Equal([new CalendarFix(families.Id, "Impact Kids", For(Audiences.Everyone))], MinistryCalendarPlan.Shape(Kids, "Impact Kids", false, [families]).Fixes);
        Assert.Equal([new CalendarFix(families.Id, "Impact Kids", MinistryViews.SeesHidden(Kids))], MinistryCalendarPlan.Shape(Kids, "Impact Kids", true, [families]).Fixes);
    }

    [Fact]
    public void CalendarsAlreadyInShapeAreLeftAloneSoRenamingTheMinistryIsTheOnlyChange()
    {
        MinistryCalendar open = Calendar("Impact Kids", For(Audiences.Everyone));
        MinistryCalendar team = Calendar("The Impact Kids Team", For(Audiences.Team));

        Assert.True(MinistryCalendarPlan.Shape(Kids, "Impact Kids", false, [open, team]).IsEmpty);
        Assert.Equal([new CalendarFix(open.Id, "Kids Church", For(Audiences.Everyone)), new CalendarFix(team.Id, "The Kids Church Team", For(Audiences.Team))],
            MinistryCalendarPlan.Shape(Kids, "Kids Church", false, [open, team]).Fixes);
    }
}
