using GSBC.ChurchApp.Features.Prayer.Api.Data;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

public sealed class PrayerRulesTests
{
    private static readonly Guid Phone = Guid.CreateVersion7();
    private static readonly Guid Person = Guid.CreateVersion7();

    [Fact]
    public void ARequestSentSignedInIsOnlyThatPersonsFromAnyPhone()
    {
        RequestRecord request = new() { InstallId = Phone, UserId = Person };

        Assert.True(PrayerRules.IsTheirs(request, Guid.CreateVersion7(), Person));
        Assert.False(PrayerRules.IsTheirs(request, Phone, null));
        Assert.False(PrayerRules.IsTheirs(request, Phone, Guid.CreateVersion7()));
    }

    [Fact]
    public void ARequestSentWithoutSigningInIsOnlyThatPhones()
    {
        RequestRecord request = new() { InstallId = Phone };

        Assert.True(PrayerRules.IsTheirs(request, Phone, null));
        Assert.False(PrayerRules.IsTheirs(request, Guid.CreateVersion7(), Person));
    }

    [Fact]
    public void StaffCanHideANameButNeverShowOneThePersonDidNotAllow()
    {
        Assert.False(PrayerRules.ShowsNameOnTheWall(new RequestRecord { Name = "Sam Sample", ShowName = false }, staffShowName: true));
        Assert.False(PrayerRules.ShowsNameOnTheWall(new RequestRecord { Name = "Sam Sample", ShowName = true }, staffShowName: false));
        Assert.True(PrayerRules.ShowsNameOnTheWall(new RequestRecord { Name = "Sam Sample", ShowName = true }, staffShowName: true));
    }

    [Theory]
    [InlineData("Sarah Kelly", "Sarah K.")]
    [InlineData("  Mary Anne  van der berg ", "Mary B.")]
    [InlineData("Sarah", "Sarah")]
    [InlineData("", "")]
    public void TheWallShowsAFirstNameAndTheInitialOfTheLastNeverTheWholeName(string name, string shown) =>
        Assert.Equal(shown, PrayerRules.ShortName(name));
}
