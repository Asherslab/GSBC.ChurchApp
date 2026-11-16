using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class TeamNamesTests
{
    [Theory]
    [InlineData("Discipleship", "The Discipleship Team")]
    [InlineData(" Impact Kids ", "The Impact Kids Team")]
    [InlineData("Welcome Team", "The Welcome Team")]
    [InlineData("The Hospitality team", "The Hospitality team")]
    [InlineData("Steam", "The Steam Team")]
    [InlineData("", "The team")]
    public void AMinistrysTeamIsNamedForItWithoutSayingTheOrTeamTwice(string ministry, string team) =>
        Assert.Equal(team, TeamNames.Of(ministry));

    [Fact]
    public void MidSentenceItStartsWithALowerCaseThe() =>
        Assert.Equal("the Discipleship Team", TeamNames.InSentence("Discipleship"));
}
