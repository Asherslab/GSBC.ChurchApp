using System.Security.Claims;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Http;
using static GSBC.ChurchApp.Features.Access.Api.Tests.TestChurch;

namespace GSBC.ChurchApp.Features.Access.Api.Tests;

public sealed class GrantTests
{
    private readonly TestChurch _church = new();

    [Fact]
    public async Task ScopesIncludesAndDeniesDecideWhatSomeoneMayDo()
    {
        Guid member = _church.AddRole("Member", ScopeKind.Ministry, ["ministries.news.read"]);
        Guid leader = _church.AddRole("Leader", ScopeKind.Ministry, ["ministries.*", "-ministries.roster.edit"], includes: [member]);
        Guid person = Guid.CreateVersion7();
        _church.Assign(person, leader, scopeObjectId: "42");

        GrantSet grants = await _church.GrantsAsync(person);

        Assert.True(grants.IsAllowed(PostNews, "42"));
        Assert.True(grants.IsAllowed(ReadNews, "42"));
        Assert.False(grants.IsAllowed(EditRoster, "42"));
        Assert.False(grants.IsAllowed(PostNews, "7"));
        Assert.False(grants.IsAllowed(AccessPermissions.View));
    }

    [Fact]
    public async Task ARoleForOneThingBringsWhatItsJobNeedsFromElsewhereEverywhere()
    {
        Guid person = Guid.CreateVersion7();
        _church.Assign(person, _church.SystemRole("publicity").Id, scopeObjectId: Music);

        GrantSet grants = await _church.GrantsAsync(person);

        Assert.True(grants.IsAllowed(PostNews, Music));
        Assert.False(grants.IsAllowed(PostNews, "youth"));
        Assert.True(grants.IsAllowed(SeePictures));
    }

    [Fact]
    public async Task HoldingARoleAnywherePutsSomeoneInThatRolesAudienceAndInTheAudienceOfRolesItIncludes()
    {
        Guid member = _church.AddRole("Member", ScopeKind.Ministry, ["ministries.news.read"]);
        Guid leader = _church.AddRole("Leader", ScopeKind.Ministry, ["ministries.news.post"], includes: [member]);
        Guid other = _church.AddRole("Welcomer", ScopeKind.Global, []);
        Guid person = Guid.CreateVersion7();
        _church.Assign(person, leader, scopeObjectId: Music);

        GrantSet grants = await _church.GrantsAsync(person);

        Assert.True(grants.IsAllowed(RoleMembership.Of(leader)));
        Assert.True(grants.IsAllowed(RoleMembership.Of(member)));
        Assert.False(grants.IsAllowed(RoleMembership.Of(other)));
        Assert.False((await _church.GrantsAsync(Guid.CreateVersion7())).IsAllowed(RoleMembership.Of(leader)));
    }

    [Fact]
    public async Task AFeatureCanAddItsPermissionsToARoleAnotherFeatureDeclares()
    {
        Guid person = Guid.CreateVersion7();
        _church.Assign(person, _church.SystemRole("member").Id, scopeObjectId: Music);

        GrantSet grants = await _church.GrantsAsync(person);

        Assert.True(grants.IsAllowed(ReadNews, Music));
        Assert.True(grants.IsAllowed(ReceiveNotices, Music));
    }

    [Fact]
    public async Task ALeaderLeadsTheMinistrysTeamsButAMemberIsOnlyAMemberWhereTheyJoined()
    {
        Guid leader = Guid.CreateVersion7(), member = Guid.CreateVersion7();
        _church.Assign(leader, _church.SystemRole("leader").Id, scopeObjectId: Music);
        _church.Assign(member, _church.SystemRole("member").Id, scopeObjectId: Music);

        GrantSet leads = await _church.GrantsAsync(leader);
        GrantSet joined = await _church.GrantsAsync(member);

        Assert.True(leads.IsAllowed(PostNews, Choir));
        Assert.False(leads.IsAllowed(PostNews, "youth"));
        Assert.True(joined.IsAllowed(ReadNews, Music));
        Assert.False(joined.IsAllowed(ReadNews, Choir));
    }

    [Fact]
    public async Task NobodyCanHandOutPermissionsTheyDoNotHold()
    {
        Guid manager = Guid.CreateVersion7();
        _church.Assign(manager, _church.SystemRole("access-manager").Id);
        GrantSet actor = await _church.GrantsAsync(manager);

        Assert.Contains(ManageAccounts, RoleGrants.NotHeld(actor, ["*"], ScopeKind.Global, null, _church.Catalog));
        Assert.Equal([ManageAccounts], RoleGrants.NotHeld(actor, ["accounts.*"], ScopeKind.Global, null, _church.Catalog));
        Assert.Empty(RoleGrants.NotHeld(actor, ["access.*", "accounts.view"], ScopeKind.Global, null, _church.Catalog));
        Assert.Empty(RoleGrants.NotHeld(actor, ["access.assignments.edit"], ScopeKind.Ministry, "42", _church.Catalog));
    }

    [Fact]
    public async Task AToolActingForAnOwnerIsRefusedEverythingSensitiveEvenIndirectly()
    {
        Guid owner = Guid.CreateVersion7();
        _church.Assign(owner, _church.SystemRole(AccessService.OwnerKey).Id);
        ClaimsPrincipal tool = new(new ClaimsIdentity(
            [new Claim(Caller.UserIdClaim, owner.ToString()), new Claim(Caller.ToolClaim, bool.TrueString)], "test"));
        ToolCallers callers = new(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = tool } });
        GrantSet compiled = await _church.GrantsAsync(owner);

        Assert.True(Caller.IsTool(tool));
        Assert.True(callers.ActsThroughATool(owner));
        Assert.False(callers.ActsThroughATool(Guid.CreateVersion7()));
        GrantSet throughTheTool = AccessEvaluator.AsSeenBy(compiled, throughATool: true, _church.Catalog);
        Assert.True(AccessEvaluator.AsSeenBy(compiled, throughATool: false, _church.Catalog).IsAllowed(ManageAccounts));
        Assert.False(throughTheTool.IsAllowed(ManageAccounts));
        Assert.False(throughTheTool.IsAllowed(AccessPermissions.EditAssignments, Music));
        Assert.True(throughTheTool.IsAllowed(PostNews, Music));
        Assert.NotNull(await new AssignmentPolicy(_church.Hierarchy, _church.Catalog).RefusalAsync(
            new Assigner(throughTheTool, true, _church.Of(owner)), _church.SystemRole("member"), Music, _church.Roles, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task APhoneFollowsWithoutAnAccountAndIsFoundWithThePeopleWhoHoldThatPermission()
    {
        Guid phone = Guid.CreateVersion7(), leader = Guid.CreateVersion7();
        Guid follower = _church.SystemRole("follower").Id;

        RoleAssignments.PhoneHoldable(_church.SystemRole("follower"), Music);
        Assert.Throws<InvalidOperationException>(() => RoleAssignments.PhoneHoldable(_church.SystemRole("member"), Music));

        (Guid, Guid, string?)[] people = [(leader, _church.SystemRole("leader").Id, Music)];
        (Guid, Guid, string?)[] phones = [(phone, follower, Music)];
        PermissionHolders music = await HoldersAsync(people, phones, Music);
        Assert.Equal([phone], music.Phones);
        Assert.Equal([leader], music.People);
        Assert.Empty((await HoldersAsync(people, phones, "youth")).Phones);
    }

    private Task<PermissionHolders> HoldersAsync(
        IReadOnlyCollection<(Guid, Guid, string?)> people, IReadOnlyCollection<(Guid, Guid, string?)> phones, string objectId) =>
        PermissionHolderLookup.HoldersAsync(people, phones, _church.Roles, ReceiveNotices, objectId, _church.Hierarchy, TestContext.Current.CancellationToken);

    [Fact]
    public async Task ALeaderLeadsEveryMinistryUnderTheirsHoweverDeepAndGivesRolesThere()
    {
        Guid leader = Guid.CreateVersion7(), helper = Guid.CreateVersion7();
        _church.Assign(leader, _church.SystemRole("leader").Id, scopeObjectId: Music);

        GrantSet leads = await _church.GrantsAsync(leader);

        Assert.True(leads.IsAllowed(PostNews, Altos));
        Assert.Null(await _church.GiveAsync(leader, helper, "member", Altos));
        Assert.NotNull(await _church.GiveAsync(helper, leader, "member", Music));
    }

    [Fact]
    public async Task AParentHoldsWhatTheirChildsRoleGivesParentsWhereTheChildHoldsIt()
    {
        Guid parent = Guid.CreateVersion7(), child = Guid.CreateVersion7(), stranger = Guid.CreateVersion7();
        _church.Assign(parent, _church.SystemRole("parent").Id, scopeObjectId: child.ToString());
        _church.Assign(child, _church.SystemRole("attendee").Id, scopeObjectId: Music);

        GrantSet parents = await _church.GrantsAsync(parent);

        Assert.True(parents.IsAllowed(ReceiveNotices, Music));
        Assert.False(parents.IsAllowed(ReadNews, Music));
        Assert.False(parents.IsAllowed(ReceiveNotices, Choir));
        Assert.False((await _church.GrantsAsync(stranger)).IsAllowed(ReceiveNotices, Music));
    }

    [Fact]
    public async Task ARoleAtAMinistryReachesWhatIsAttachedToIt()
    {
        Guid leader = Guid.CreateVersion7();
        _church.Assign(leader, _church.SystemRole("leader").Id, scopeObjectId: Music);
        Permission editCalendar = new("ministries.news.post", ScopeKind.Calendar, "Post news");

        GrantSet leads = await _church.GrantsAsync(leader, new ChoirCalendar());

        Assert.True(leads.IsAllowed(editCalendar, "choir-calendar"));
        Assert.False(leads.IsAllowed(editCalendar, "youth-calendar"));
    }

    private sealed class ChoirCalendar : IScopeAttachments
    {
        public ScopeKind AttachedTo => ScopeKind.Ministry;

        public ValueTask<IReadOnlyList<Scope>> AttachedAsync(string objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<Scope>>(objectId == Choir ? [Scope.Of(ScopeKind.Calendar, "choir-calendar")] : []);
    }
}
