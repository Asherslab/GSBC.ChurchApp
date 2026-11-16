namespace GSBC.ChurchApp.Features.News.Client.Tests;

public sealed class TellAgainWordsTests
{
    [Theory]
    [InlineData("Working bee this Saturday")]
    [InlineData("Updated: Working bee this Saturday")]
    [InlineData("Updated: Updated: Working bee this Saturday")]
    public void TellingPeopleAgainMarksTheWordsAsUpdatedOnceHoweverOftenItIsSent(string words) =>
        Assert.Equal("Updated: Working bee this Saturday", NewsWords.Updated(words));
}
