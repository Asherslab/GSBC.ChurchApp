using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class HomeTopTests
{
    [Fact]
    public void HomeStartsWithTheChurchsNameAndTodaysDateUntilADraftChangesThem()
    {
        Assert.Equal(new HomeTop("Good Shepherd", HomeTopLine.Date), HubSurfaces.Top.In([]));
    }

    [Theory]
    [InlineData(HubSurfaces.Top.Today, "Welcome home", "today")]
    [InlineData(HubSurfaces.Top.Words, "Welcome home", "own")]
    [InlineData(HubSurfaces.Top.Words, "", "none")]
    [InlineData(HubSurfaces.Top.Nothing, "Welcome home", "none")]
    public void TheLineUnderTheTitleIsTheDateTheStaffsOwnWordsOrNothing(string under, string line, string shown)
    {
        HomeTop top = HubSurfaces.Top.Of(new SettingValues(new Dictionary<string, string>
        {
            [HubSurfaces.Top.Title.Key] = "Easter at Good Shepherd",
            [HubSurfaces.Top.Under.Key] = under,
            [HubSurfaces.Top.Line.Key] = line,
        }));

        Assert.Equal("Easter at Good Shepherd", top.Title);
        Assert.Equal(shown, top.Line switch
        {
            HomeTopLine.Today => "today",
            HomeTopLine.Own own when own.Words == line => "own",
            HomeTopLine.Nothing => "none",
            _ => "wrong",
        });
    }
}
