using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Access.Contracts;
using GSBC.ChurchApp.Platform.Permissions;
using static GSBC.ChurchApp.Features.Access.Api.Tests.TestChurch;

namespace GSBC.ChurchApp.Features.Access.Api.Tests;

public sealed class AccessExplanationTests
{
    private readonly TestChurch _church = new();

    [Fact]
    public async Task APreviewSaysWhoGainsAndLosesWhatBeforeARoleChanges()
    {
        Guid roleId = _church.AddRole("Roster helper", ScopeKind.Ministry, ["ministries.roster.edit"]);
        Guid helper = Guid.CreateVersion7();
        _church.Assign(helper, roleId, scopeObjectId: Music);
        RoleRecord changed = new()
        {
            Id = roleId, Name = "Roster helper", ScopeKind = ScopeKind.Ministry.Name,
            Patterns = [new RolePattern { RoleId = roleId, Pattern = "ministries.news.post" }],
        };

        RoleEffect effect = await Explanations().PreviewAsync(_church.Of(helper), _church.Roles, roleId, changed, TestContext.Current.CancellationToken);

        PersonEffect person = Assert.Single(effect.People);
        Assert.Equal(helper.ToString(), person.UserId);
        Assert.Equal(("ministries.news.post", Music), Assert.Single(person.Gains.Select(change => (change.Node, change.ObjectId))));
        Assert.Equal(("ministries.roster.edit", Music), Assert.Single(person.Losses.Select(change => (change.Node, change.ObjectId))));
    }

    [Fact]
    public async Task WhatSomeoneCanDoIsGroupedByPlaceWithTheRoleThatGivesIt()
    {
        Guid person = Guid.CreateVersion7();
        _church.Assign(person, _church.SystemRole("account-manager").Id);
        _church.Assign(person, _church.SystemRole("leader").Id, scopeObjectId: Music);

        PersonAbilities abilities = await Explanations().ExplainAsync(_church.Of(person), _church.Roles, TestContext.Current.CancellationToken);

        PlaceAbilities everywhere = Assert.Single(abilities.Places, place => place.ObjectId is null);
        Assert.Equal(["accounts.manage", "accounts.view"], everywhere.Nodes);
        Assert.Equal("Account manager", Assert.Single(everywhere.Because).RoleName);
        PlaceAbilities music = Assert.Single(abilities.Places, place => place.ObjectId == Music);
        Assert.True(music.IncludesTeams);
        Assert.Contains("ministries.news.post", music.Nodes);
        Assert.Equal("Leader", Assert.Single(music.Because).RoleName);
    }

    private AccessExplanations Explanations() => new(_church.Hierarchy, _church.Catalog);
}
