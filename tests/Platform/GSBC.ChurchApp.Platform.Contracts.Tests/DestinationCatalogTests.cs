using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class DestinationCatalogTests
{
    private static readonly DestinationArea Events = new("events", "Events", "calendar", 10);
    private static readonly DestinationPage Settings = new("/notifications/settings", "Notification settings", "sliders", DestinationAreas.Pages, "More", Audience.Everyone);
    private static readonly DestinationKind Event = new("events.event", Events, "Event", "calendar", "events");

    private readonly DestinationCatalog _catalog = new([ShellDestinations.All, new DestinationSet([Settings], [Event])]);

    [Theory]
    [InlineData("/notifications/settings")]
    [InlineData("/notifications/settings/")]
    [InlineData("/notifications/settings?from=inbox")]
    [InlineData("/open/notifications/settings")]
    [InlineData("https://app.baptist.com.au/open/notifications/settings")]
    public void ALinkFindsItsPageHoweverItIsWritten(string link) =>
        Assert.Equal(Settings, Assert.IsType<ResolvedPage>(_catalog.Resolve(link)).Page);

    [Fact]
    public void TheSharedLinkToHomeIsHome()
    {
        Assert.Equal(ShellDestinations.Home, Assert.IsType<ResolvedPage>(_catalog.Resolve("/open")).Page);
        Assert.Equal("https://app.baptist.com.au/open", DestinationPaths.Shareable(new Uri("https://app.baptist.com.au/"), "/"));
        Assert.Equal("https://app.baptist.com.au/open/events/42", DestinationPaths.Shareable(new Uri("https://app.baptist.com.au/"), Event.PathOf("42")));
    }

    [Fact]
    public void AnItemIsItsKindAndId()
    {
        ResolvedItem item = Assert.IsType<ResolvedItem>(_catalog.Resolve("/open/events/a%20b"));

        Assert.Equal(Event, item.Kind);
        Assert.Equal("a b", item.Id);
        Assert.Equal("/events/a%20b", item.Path);
    }

    [Theory]
    [InlineData("/sermons/1")]
    [InlineData("/events")]
    [InlineData("/events/1/extra")]
    [InlineData("//evil.example/events/1")]
    [InlineData("mailto:office@example.com")]
    public void AnythingElseIsNotAPlace(string link) => Assert.Null(_catalog.Resolve(link));

    [Fact]
    public void TwoFeaturesCannotClaimTheSamePlace()
    {
        Assert.Throws<ArgumentException>(() => new DestinationCatalog([ShellDestinations.All, ShellDestinations.All]));
        Assert.Throws<ArgumentException>(() => new DestinationCatalog([new DestinationSet([Settings], [Event, Event])]));
    }

    [Fact]
    public void AListAndItsItemsShareAPrefix()
    {
        DestinationPage list = new("/events", "Events", "calendar", Events, "", Audience.Everyone);
        DestinationCatalog catalog = new([new DestinationSet([list], [Event])]);

        Assert.Equal(list, Assert.IsType<ResolvedPage>(catalog.Resolve("/events")).Page);
        Assert.Equal(Event, Assert.IsType<ResolvedItem>(catalog.Resolve("/events/42")).Kind);
    }

    [Theory]
    [InlineData("everyone", null, "everyone", true)]
    [InlineData("signed-in", null, "everyone", false)]
    [InlineData("signed-in", null, "signed-in", true)]
    [InlineData("permission:media.view", null, "signed-in", false)]
    [InlineData("permission:media.view", null, "permission:media.view", true)]
    [InlineData("permission:media.view", "ministry:music", "permission:media.view", false)]
    public void AnAnnouncementOnlyOpensWhatEveryoneItReachesCanOpen(string place, string? scope, string viewers, bool openable)
    {
        Assert.True(Audience.TryParse(place, out Audience? placeAudience));
        Assert.True(Audience.TryParse(viewers, out Audience? viewerAudience));

        Assert.Equal(openable, DestinationAudiences.OpenableByAll(placeAudience, scope, viewerAudience));
    }
}
