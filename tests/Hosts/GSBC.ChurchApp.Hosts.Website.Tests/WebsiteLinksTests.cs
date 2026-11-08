namespace GSBC.ChurchApp.Hosts.Website.Tests;

public sealed class WebsiteLinksTests
{
    private static readonly WebsiteLinks Links = new(new WebsiteOptions
    {
        EventsPage = "https://church.example/events",
        SermonsPage = "https://church.example/sermons",
        AppOrigin = "https://app.church.example",
    });

    [Theory]
    [InlineData("/sermons/9f2c", "https://church.example/sermons?sermon=9f2c")]
    [InlineData("/sermons/series/rooted", "https://church.example/sermons?series=rooted")]
    [InlineData("/sermons/speakers/ab12", "https://church.example/sermons?speaker=ab12")]
    [InlineData("/sermons/series", "https://church.example/sermons?view=series")]
    [InlineData("/sermons?q=hope&page=2", "https://church.example/sermons?q=hope&page=2")]
    [InlineData("/events/abc-2026-11-14?calendar=kids", "https://church.example/events?event=abc-2026-11-14&calendar=kids")]
    [InlineData("/events?calendar=kids&show=kids", "https://church.example/events?calendar=kids&show=kids")]
    public void APageIsSharedAsTheWebsitePageThatShowsIt(string embedPage, string website) =>
        Assert.Equal(website, Links.OnWebsite(embedPage));

    [Fact]
    public void CalendarLinksSubscribeThroughTheAppHost()
    {
        Assert.Equal("webcal://app.church.example/calendars/kids.ics", Links.Subscribe("kids"));
        Assert.Equal("webcal://app.church.example/calendars/public.ics", Links.Subscribe(null));
    }
}
