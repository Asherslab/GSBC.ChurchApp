using GSBC.ChurchApp.Features.Access.Contracts;
using static GSBC.ChurchApp.Features.Access.Api.Tests.TestChurch;

namespace GSBC.ChurchApp.Features.Access.Api.Tests;

public sealed class AccessExplanationTests
{
    private readonly TestChurch _church = new();

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
