using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class HiddenMinistryRuleTests
{
    private readonly MinistryRecord _office = new() { Id = Guid.CreateVersion7(), Name = "Church office", Hidden = true };
    private readonly MinistryRecord _accounts;
    private readonly MinistryRecord _payroll;
    private readonly MinistryRecord _music = new() { Id = Guid.CreateVersion7(), Name = "Music" };
    private readonly MinistryRecord _lifeGroups = new() { Id = Guid.CreateVersion7(), Name = "Life Groups" };
    private readonly MinistryRecord _summerFit;

    public HiddenMinistryRuleTests()
    {
        _accounts = new() { Id = Guid.CreateVersion7(), ParentId = _office.Id, Name = "Accounts" };
        _payroll = new() { Id = Guid.CreateVersion7(), ParentId = _accounts.Id, Name = "Payroll" };
        _summerFit = new() { Id = Guid.CreateVersion7(), ParentId = _lifeGroups.Id, Name = "Summer Fit", ShowsInLifeGroups = true };
    }

    private MinistryTree Tree => new([_office, _accounts, _payroll, _music, _lifeGroups, _summerFit]);

    [Fact]
    public void EverythingUnderAHiddenMinistryIsHiddenAndSaysWhichOneHidesIt()
    {
        Assert.True(Tree.IsHidden(_payroll.Id));
        Assert.Equal(_office.Id, Tree.HiddenBy(_payroll.Id)?.Id);
        Assert.Null(Tree.HiddenBy(_office.Id));
        Assert.False(Tree.IsHidden(_music.Id));
    }

    [Fact]
    public void OnlyItsOwnPeopleThoseUnderItAndStaffSeeAHiddenMinistry()
    {
        GrantSet payrollTeam = At(_payroll, MinistriesPermissions.SeeHiddenNode);
        GrantSet officeTeamOnly = At(_office, MinistriesPermissions.SeeHiddenNode);
        GrantSet staff = new([new Grant(PermissionPattern.Parse("ministries.*"), Scope.Global)]);
        GrantSet musicTeam = At(_music, MinistriesPermissions.SeeHiddenNode, MinistriesPermissions.TeamViewNode);

        Assert.True(MinistryViews.MaySeeMinistry(payrollTeam, Tree, _office.Id));
        Assert.True(MinistryViews.MaySeeMinistry(payrollTeam, Tree, _payroll.Id));
        Assert.False(MinistryViews.MaySeeMinistry(officeTeamOnly, Tree, _accounts.Id));
        Assert.True(MinistryViews.MaySeeMinistry(staff, Tree, _payroll.Id));
        Assert.False(MinistryViews.MaySeeMinistry(musicTeam, Tree, _office.Id));
        Assert.False(MinistryViews.MaySeeMinistry(GrantSet.Empty, Tree, _accounts.Id));
        Assert.True(MinistryViews.MaySeeMinistry(GrantSet.Empty, Tree, _music.Id));
    }

    [Fact]
    public void ALifeGroupIsForEveryoneSoItIsNeverHiddenNorUnderAHiddenMinistry()
    {
        Assert.NotNull(MinistryRules.WhyNotHide(Tree, _summerFit.Id, _lifeGroups.Id, Details(_summerFit, hidden: true)));
        Assert.NotNull(MinistryRules.WhyNotHide(Tree, _lifeGroups.Id, null, Details(_lifeGroups, hidden: true)));
        Assert.NotNull(MinistryRules.WhyNotHide(Tree, _accounts.Id, _office.Id, Details(_accounts, hidden: false, lifeGroup: true)));
        Assert.NotNull(MinistryRules.WhyNotMove(Tree, _summerFit, _accounts, _ => true));
        Assert.Null(MinistryRules.WhyNotHide(Tree, _music.Id, null, Details(_music, hidden: true)));
        Assert.Null(MinistryRules.WhyNotMove(Tree, _music, _office, _ => true));
    }

    [Fact]
    public void MovingUnderAHiddenMinistryHidesItAndMovingOutShowsItAgain()
    {
        Assert.Equal(_office.Id, MoveEffects.Of(Tree, _music, _accounts).HiddenBy?.Id);
        Assert.Null(MoveEffects.Of(Tree, _accounts, null).HiddenBy);
    }

    private static GrantSet At(MinistryRecord ministry, params string[] nodes) =>
        new(nodes.Select(node => new Grant(PermissionPattern.Parse(node), Scope.Of(ScopeKind.Ministry, ministry.Id.ToString()))).ToList());

    private static MinistryDetails Details(MinistryRecord ministry, bool hidden, bool lifeGroup = false) => new()
    {
        Id = ministry.Id.ToString(), Name = ministry.Name, Hidden = hidden, ShowsInLifeGroups = lifeGroup || ministry.ShowsInLifeGroups,
    };
}
