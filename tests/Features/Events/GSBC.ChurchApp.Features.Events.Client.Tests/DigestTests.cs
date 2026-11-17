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

    [Fact]
    public void CopyForEmailGivesABlockElvantosBuilderCanHoldStyledInlineButOneLightOnlyRuleAndButtonsMailClientsDraw()
    {
        EmailItem highTea = new(DigestItemKind.Event, new DigestWords { Title = "High tea", RegisterLink = "https://example.org/high-tea" }, null, "", "");

        string html = DigestEmail.Html("Monday 9 – Sunday 22 November", [highTea], "https://dev-app.example.org/open/week");

        Assert.StartsWith("<style>:root{color-scheme:light only;supported-color-schemes:light only}</style><table", html);
        Assert.DoesNotContain("<html", html);
        Assert.DoesNotContain("<body", html);
        Assert.Single(html.Split("<style")[1..]);
        Assert.DoesNotContain("class=", html);
        Assert.Equal(2, html.Split("<td bgcolor=\"#2A3D5E\" style=\"background:#2A3D5E;border-radius:20px\"><a href=").Length - 1);
    }

    [Fact]
    public void TheEmailSitsCentredOnAFullWidthBackgroundWithEachPictureWhereStaffChoseAndAPostLinkingToItself()
    {
        EmailItem beside = new(DigestItemKind.Event, new DigestWords { Title = "Baptisms" }, "https://media.example.org/baptisms.jpg", "", "");
        EmailItem across = beside with { Words = new DigestWords { Title = "Carols" }, PictureUrl = "https://media.example.org/carols.jpg", PictureAt = DigestPictureAt.Across };
        EmailItem post = new(DigestItemKind.News, new DigestWords { Title = "Christmas offering" }, null, "", "", Link: "https://dev-app.example.org/open/news/offering");

        string html = DigestEmail.Html("Monday 14 – Sunday 27 December", [beside, across, post], "https://dev-app.example.org/open/week", "https://media.example.org/church.jpg");

        Assert.Contains("</style><table role=\"presentation\" width=\"100%\"", html);
        Assert.Contains("width=\"100%\" align=\"center\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" bgcolor=\"#ffffff\" style=\"width:100%;max-width:600px", html);
        Assert.True(html.IndexOf("church.jpg", StringComparison.Ordinal) < html.IndexOf("What's Next at Good Shepherd", StringComparison.Ordinal));
        Assert.Matches("<td width=\"34%\"[^>]*><img src=\"https://media.example.org/baptisms.jpg\"", html);
        Assert.DoesNotMatch("<td width=\"34%\"[^>]*><img src=\"https://media.example.org/carols.jpg\"", html);
        Assert.Contains("href=\"https://dev-app.example.org/open/news/offering\"", html);
        Assert.Contains(">Read the post</a>", html);
    }

    [Theory]
    [InlineData(null, DigestHeading.Nothing, true, true)]
    [InlineData("https://media.example.org/banner.jpg", DigestHeading.TitleAndDates, true, true)]
    [InlineData("https://media.example.org/banner.jpg", DigestHeading.DatesOnly, false, true)]
    [InlineData("https://media.example.org/banner.jpg", DigestHeading.Nothing, false, false)]
    public void TheTitleCanBeLeftOutOnlyUnderATopPhotoSoTheEmailNeverStartsWithNothingNamingIt(string? topPicture, DigestHeading heading, bool title, bool dates)
    {
        EmailItem baptisms = new(DigestItemKind.Event, new DigestWords { Title = "Baptisms" }, null, "", "");

        string html = DigestEmail.Html("Monday 14 – Sunday 27 December", [baptisms], "https://dev-app.example.org/open/week", topPicture, heading);

        Assert.Equal(title, html.Contains(">What's Next at Good Shepherd<"));
        Assert.Equal(dates, html.Contains(">Monday 14 – Sunday 27 December<"));
    }

    [Fact]
    public void ABannerStandingInForTheTitleNamesTheEmailForAnyoneWhoCantSeeIt()
    {
        EmailItem baptisms = new(DigestItemKind.Event, new DigestWords { Title = "Baptisms" }, null, "", "");

        string html = DigestEmail.Html("Monday 14 – Sunday 27 December", [baptisms], "https://dev-app.example.org/open/week", "https://media.example.org/banner.jpg", DigestHeading.Nothing);

        Assert.Contains("alt=\"What&#39;s Next at Good Shepherd, Monday 14 – Sunday 27 December\"", html);
    }

    [Fact]
    public void AnEarlierDigestReadsAsItWasSentWithNothingDroppedItsWeeksNamedByDateAndEachDayNamedOutright()
    {
        WhatsNextDigest digest = new(Fortnight, [Item("agm", "2026-11-12"), Item("high-tea", "2026-11-21"), Item("own:bee", "")]);

        IReadOnlyList<WhatsNextWeek> weeks = digest.WeeksAsSent();

        Assert.Equal(["Week of 9 November", "Week of 16 November"], weeks.Select(week => week.Heading));
        Assert.Equal(["agm", "own:bee", "high-tea"], weeks.SelectMany(week => week.Items).Select(item => item.Key));
        Assert.Equal("Thursday 12 November", WhatsNextDigest.WhenAsSent(weeks[0].Items[0]));
    }

    [Fact]
    public void SomethingWrittenWithoutADateSitsInTheDigestsWeeksInStaffsOrderAndGoesWhenTheDigestEnds()
    {
        WhatsNextDigest digest = new(new DigestShown { Start = "2026-12-14", End = "2026-12-27" },
        [
            Item("no-bible-study", "2026-12-16"),
            Item("baptisms", ""),
        ]);

        IReadOnlyList<WhatsNextWeek> weeks = digest.Weeks(new DateOnly(2026, 11, 17));

        Assert.Equal(["no-bible-study", "baptisms"], Assert.Single(weeks).Items.Select(item => item.Key));
        Assert.Empty(digest.Weeks(new DateOnly(2026, 12, 28)));
    }

    private static DigestShownItem Item(string key, string date) => new() { Key = key, FirstDate = date, LastDate = date, Words = new DigestWords { Title = key } };
}
