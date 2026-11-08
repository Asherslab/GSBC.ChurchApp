using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Client.Tests;

public sealed class DigestTests
{
    private static readonly DigestShown Fortnight = new() { Start = "2026-11-09", End = "2026-11-22" };

    [Fact]
    public void TheEmailCarriesStaffWordsAsTextAndARegisterButtonOnlyForAWebLink()
    {
        EmailItem highTea = new(DigestItemKind.Event, new DigestWords { Title = "Scones <b>& carols</b>", RegisterLink = "https://example.org/high-tea" }, null, "Fellowship Area", "");
        EmailItem trick = new(DigestItemKind.Event, new DigestWords { Title = "Games night", RegisterLink = "javascript:alert(1)" }, null, "", "");

        string html = DigestEmail.Html("Monday 9 – Sunday 22 November", [highTea, trick], "https://dev-app.example.org/open/week");

        Assert.Contains("Scones &lt;b&gt;&amp; carols&lt;/b&gt;", html);
        Assert.Contains("href=\"https://example.org/high-tea\"", html);
        Assert.DoesNotContain("javascript:", html);
        Assert.Equal(1, html.Split(">Register</a>").Length - 1);
    }

    [Fact]
    public void TheAppsDigestDropsWhatHasPassedAndHeadsEachWeekFromToday()
    {
        WhatsNextDigest digest = new(Fortnight,
        [
            Item("agm", "2026-11-12"),
            Item("outreach", "2026-11-14"),
            Item("high-tea", "2026-11-21"),
        ]);

        IReadOnlyList<WhatsNextWeek> sentOnSunday = digest.Weeks(new DateOnly(2026, 11, 8));
        IReadOnlyList<WhatsNextWeek> onFriday = digest.Weeks(new DateOnly(2026, 11, 13));
        IReadOnlyList<WhatsNextWeek> nextMonday = digest.Weeks(new DateOnly(2026, 11, 16));

        Assert.Equal(["This week", "Next week"], sentOnSunday.Select(week => week.Heading));
        Assert.Equal(["This week", "Next week"], onFriday.Select(week => week.Heading));
        Assert.Equal(["outreach"], onFriday[0].Items.Select(item => item.Key));
        Assert.Equal("This week", Assert.Single(nextMonday).Heading);
        Assert.Equal("One thing coming up at Good Shepherd in the week ahead.", digest.Lead(new DateOnly(2026, 11, 16)));
        Assert.Equal("Three things coming up at Good Shepherd in the next two weeks.", digest.Lead(new DateOnly(2026, 11, 8)));
    }

    private static DigestShownItem Item(string key, string date) => new() { Key = key, FirstDate = date, LastDate = date, Words = new DigestWords { Title = key } };
}
