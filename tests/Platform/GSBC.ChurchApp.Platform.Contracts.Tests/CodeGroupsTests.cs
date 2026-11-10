namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class CodeGroupsTests
{
    [Theory]
    [InlineData("zf3b2td9", "ZF3B-2TD9")]
    [InlineData("ZF3B-2TD9", "ZF3B-2TD9")]
    [InlineData(" zf3b 2td9 ", "ZF3B-2TD9")]
    [InlineData("ZF3B", "ZF3B")]
    [InlineData("ZF3B2", "ZF3B-2")]
    [InlineData("ZF3B2TD9XY", "ZF3B-2TD9")]
    public void ACodeIsTypedOrPastedAsCapitalsWithADashEveryFourCharacters(string typed, string shown) =>
        Assert.Equal(shown, CodeGroups.Format(typed, 8));
}
