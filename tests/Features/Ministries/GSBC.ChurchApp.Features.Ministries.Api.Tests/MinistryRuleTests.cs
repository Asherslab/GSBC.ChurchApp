using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class MinistryRuleTests
{
    private static readonly MinistryRecord Music = new() { Id = Guid.CreateVersion7(), Name = "Music" };
    private static readonly MinistryRecord Choir = new() { Id = Guid.CreateVersion7(), ParentId = Music.Id, Name = "Choir" };
    private static readonly MinistryRecord Altos = new() { Id = Guid.CreateVersion7(), ParentId = Choir.Id, Name = "Altos" };
    private static readonly MinistryRecord Youth = new() { Id = Guid.CreateVersion7(), Name = "Impact Youth" };

    [Fact]
    public void MinistriesNestToAnyDepthAndCannotBeMovedUnderThemselves()
    {
        MinistryTree tree = new([Music, Choir, Altos, Youth]);

        Assert.Equal([Choir.Id, Altos.Id], tree.Below(Music.Id).Select(ministry => ministry.Id));
        Assert.Equal([Choir.Id, Music.Id], tree.Above(Altos.Id).Select(ministry => ministry.Id));
        Assert.True(tree.WouldLoop(Music.Id, Altos.Id));
        Assert.False(tree.WouldLoop(Altos.Id, Youth.Id));
    }

    [Fact]
    public void EachAudienceIsSeenOnlyByThoseHoldingItsPermissionAndTheTeamsUnderneathSeeTheTeamsThings()
    {
        static bool None(string node) => false;
        static bool Attendee(string node) => node == MinistriesPermissions.AttendeesViewNode;
        static bool Team(string node) => node == MinistriesPermissions.TeamViewNode;

        Assert.True(MinistryRules.MaySee(Audiences.Everyone, None, None));
        Assert.True(MinistryRules.MaySee(Audiences.Attendees, Attendee, None));
        Assert.False(MinistryRules.MaySee(Audiences.Parents, Attendee, None));
        Assert.False(MinistryRules.MaySee(Audiences.Team, Attendee, Attendee));
        Assert.True(MinistryRules.MaySee(Audiences.Team, None, Team));
        Assert.False(MinistryRules.MaySee(Audiences.Attendees, None, Attendee));
    }

    [Fact]
    public void JoiningFollowsTheMinistrysSettingForTheTeamOrForComingAlong()
    {
        Assert.True(MinistryRules.Decide(Joining.Open, Places.Team).Joins);
        Assert.True(MinistryRules.Decide(Joining.Ask, Places.Attending).Asks);
        Assert.Equal("Leaders invite people to this team. Ask one of them.", MinistryRules.Decide(Joining.Invite, Places.Team).Refusal);
        Assert.NotNull(MinistryRules.Decide(Joining.None, Places.Attending).Refusal);
    }

    [Fact]
    public void AMinistryWithoutAttendeesPostsOnlyForEveryoneOrItsTeam()
    {
        Assert.Equal([Audiences.Everyone, Audiences.Team], MinistryRules.PostableAudiences(Joining.None));
        Assert.Equal(Audiences.All, MinistryRules.PostableAudiences(Joining.Ask));
        Assert.NotNull(MinistryRules.WhyNotPost("Pick up by 8:30", "", Audiences.Parents, MinistryRules.PostableAudiences(Joining.None)));
        Assert.Null(MinistryRules.WhyNotPost("Pick up by 8:30", "", Audiences.Parents, MinistryRules.PostableAudiences(Joining.Open)));
    }

    [Fact]
    public void YourPlaceIsSaidInWordsLeadingFirstThenTheTeamThenYourChildren()
    {
        Assert.Equal("You lead it", MinistryRules.Yours([Roles.Team, Roles.Leader], [], leadsFromAbove: false));
        Assert.Equal("On the team", MinistryRules.Yours([Roles.Team], ["Mia"], leadsFromAbove: false));
        Assert.Equal("Mia and Leo attend", MinistryRules.Yours([], ["Mia", "Leo"], leadsFromAbove: false));
        Assert.Equal("", MinistryRules.Yours([], [], leadsFromAbove: false));
    }
}
