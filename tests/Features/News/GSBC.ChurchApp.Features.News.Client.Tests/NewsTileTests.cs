using GSBC.ChurchApp.Platform.Client.News;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.News.Client.Tests;

public sealed class NewsTileTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 4, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PostsAfterTheLastLookAtNewsAreNew()
    {
        NewsTileContent content = NewsTileContent.Of([Posted("before", hours: -30), Posted("after", hours: -2), Posted("newest", hours: -1)],
            Now, lastLookedUtc: Now.AddHours(-3));

        Assert.Equal(2, content.New);
        Assert.Equal(["newest", "after", "before"], content.Newest.Select(item => item.Title));
    }

    [Fact]
    public void BeforeTheFirstLookAtNewsTheLastWeekIsNew()
    {
        NewsTileContent content = NewsTileContent.Of([Posted("old", hours: -24 * 8), Posted("this week", hours: -24 * 6)], Now, lastLookedUtc: null);

        Assert.Equal(1, content.New);
    }

    [Fact]
    public void AnUrgentPostFromTodayStandsApartFromTheNewest()
    {
        NewsTileContent content = NewsTileContent.Of(
            [Posted("urgent today", hours: -1, urgent: true), Posted("urgent last week", hours: -24 * 6, urgent: true), Posted("lead", hours: -2)],
            Now, lastLookedUtc: Now.AddHours(-12));

        Assert.Equal("urgent today", content.Urgent?.Title);
        Assert.Equal(["lead", "urgent last week"], content.Newest.Select(item => item.Title));
        Assert.Equal(1, content.New);
    }

    private static NewsItem Posted(string title, int hours, bool urgent = false) =>
        new(title, title, new RichText(), null, Now.AddHours(hours), null, "Church office", "", $"news/{title}", urgent);
}
