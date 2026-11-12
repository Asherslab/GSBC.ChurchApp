using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class MinistryTabsTests
{
    private static readonly MinistryRecord Group = new() { Id = Guid.CreateVersion7(), Name = "A group", ShowsInLifeGroups = true };

    [Fact]
    public void ATabWithOnlyTeamThingsIsHiddenFromVisitorsAndAttendeesAndShownToTheTeam()
    {
        MinistryTree tree = new([Group]);
        string[] parts = [MinistryParts.News, MinistryParts.Calendar, MinistryParts.Files];
        GrantSet visitor = GrantSet.Empty;
        GrantSet attendee = Holding(MinistriesPermissions.AttendeesView);
        GrantSet team = Holding(MinistriesPermissions.TeamView, MinistriesPermissions.AttendeesView, MinistriesPermissions.ParentsView);

        Assert.All(parts, part => Assert.False(MinistryParts.HasTab(PageFor(visitor, tree), part)));
        Assert.All(parts, part => Assert.False(MinistryParts.HasTab(PageFor(attendee, tree), part)));
        Assert.All(parts, part => Assert.True(MinistryParts.HasTab(PageFor(team, tree), part)));
        Assert.True(MinistryParts.HasTab(PageFor(attendee, tree, you: new YourPlace { MayPostFor = [Audiences.Everyone] }), MinistryParts.Files));
        Assert.True(MinistryParts.HasTab(PageFor(attendee, tree, you: new YourPlace { MayEditSettings = true }), MinistryParts.Calendar));
    }

    [Fact]
    public void ANewMinistryStartsWithOnlyNewsCalendarAndChatsOn() =>
        Assert.Equal([MinistryParts.News, MinistryParts.Calendar, MinistryParts.Chats],
            MinistryParts.All.Select(part => part.Key).Where(part => MinistryParts.IsOn(MinistryParts.OffWhenNew(), part)));

    [Fact]
    public void TabsFollowTheMinistrysOrderWithAnyNewPartAtTheEnd() =>
        Assert.Equal([MinistryParts.Chats, MinistryParts.Rosters, MinistryParts.News, MinistryParts.Calendar, MinistryParts.Files],
            MinistryParts.Ordered([MinistryParts.Chats, "gone", MinistryParts.Rosters, MinistryParts.Chats]));

    private static MinistryPage PageFor(GrantSet grants, MinistryTree tree, YourPlace? you = null)
    {
        bool sees = MinistryViews.MaySee(Audiences.Team, grants, tree, Group.Id);
        return new MinistryPage
        {
            Ministry = new MinistryDetails { Id = Group.Id.ToString() },
            Posts = sees ? [new Post { Audience = Audiences.Team }] : [],
            Calendars = sees ? [new CalendarLine { Audience = Audiences.Team }] : [],
            Files = sees ? [new FileItem { Audience = Audiences.Team }] : [],
            You = you ?? new YourPlace(),
        };
    }

    private static GrantSet Holding(params Permission[] permissions) =>
        new([.. permissions.Select(permission => new Grant(PermissionPattern.Parse(permission.Node), Scope.Of(ScopeKind.Ministry, Group.Id.ToString())))]);
}
