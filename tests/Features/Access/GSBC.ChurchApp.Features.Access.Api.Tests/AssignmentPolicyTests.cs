using GSBC.ChurchApp.Features.Access.Api.Data;
using static GSBC.ChurchApp.Features.Access.Api.Tests.TestChurch;

namespace GSBC.ChurchApp.Features.Access.Api.Tests;

public sealed class AssignmentPolicyTests
{
    private readonly TestChurch _church = new();

    [Fact]
    public async Task LeadersAddMembersButOnlyStaffMakeLeaders()
    {
        Guid staff = Guid.CreateVersion7(), musicLeader = Guid.CreateVersion7(), choirMember = Guid.CreateVersion7(), newcomer = Guid.CreateVersion7();
        _church.Assign(staff, _church.SystemRole("staff").Id);
        _church.Assign(choirMember, _church.SystemRole("member").Id, scopeObjectId: Choir);

        Assert.Null(await _church.GiveAsync(staff, musicLeader, "leader", Music));
        Assert.Null(await _church.GiveAsync(musicLeader, newcomer, "member", Choir));

        Assert.Equal("Only someone with the Staff role can give the Leader role. Ask one of them.",
            await _church.GiveAsync(musicLeader, newcomer, "leader", Choir));
        Assert.NotNull(await _church.GiveAsync(choirMember, newcomer, "member", Choir));
        Assert.NotNull(await _church.GiveAsync(musicLeader, newcomer, "member", "youth"));
    }

    [Fact]
    public async Task NobodyGivesARoleThatDoesMoreThanTheyCan()
    {
        Guid accessManager = Guid.CreateVersion7();
        _church.Assign(accessManager, _church.SystemRole("access-manager").Id);

        string? refusal = await _church.GiveAsync(accessManager, Guid.CreateVersion7(), "leader", Music);

        Assert.StartsWith("You can't give the Leader role, because it lets people do things you can't do yourself: ", refusal);
        Assert.Contains("Post news", refusal);
    }

    [Fact]
    public void TheLastActiveOwnerCannotBeRemoved()
    {
        RoleRecord owner = _church.SystemRole(AccessService.OwnerKey);
        Assignment current = _church.Assign(Guid.CreateVersion7(), owner.Id);
        Assignment lapsed = _church.Assign(Guid.CreateVersion7(), owner.Id, expiresAt: _church.Now.AddDays(-1));

        Assert.NotNull(AssignmentPolicy.LastOwner(owner, [current], [current], _church.Now));
        Assert.Null(AssignmentPolicy.LastOwner(owner, [lapsed], [current], _church.Now));

        Assignment second = _church.Assign(Guid.CreateVersion7(), owner.Id);

        Assert.Null(AssignmentPolicy.LastOwner(owner, [current], [current, second], _church.Now));
        Assert.Null(AssignmentPolicy.LastOwner(owner, [second], [current, second], _church.Now));
    }
}
