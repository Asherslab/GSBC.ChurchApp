using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class FollowingTests
{
    [Fact]
    public void WhatAPhoneFollowsIsTheFollowingRolesItHolds()
    {
        string harbour = Guid.CreateVersion7().ToString();

        Follows follows = SermonFollowing.PhoneFollows([
            new HeldRole(SermonsPermissions.SeriesFollowerRole, harbour, null),
            new HeldRole("ministries.member", "youth", null),
        ]);

        Assert.Equal([harbour], follows.SeriesIds);
        Assert.False(follows.NewSermons);
        Assert.True(SermonFollowing.PhoneFollows([new HeldRole(SermonsPermissions.FollowerRole, null, null)]).NewSermons);
    }
}
