using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class WebsiteHostsTests
{
    [Theory]
    [InlineData("https://app.baptist.com.au/open/week", "https://embed.baptist.com.au")]
    [InlineData("https://uat-app.baptist.com.au/open/week", "https://uat-embed.baptist.com.au")]
    [InlineData("https://dev-app.baptist.com.au/", "https://dev-embed.baptist.com.au")]
    public void EachEnvironmentsAppHostLeadsToItsOwnWebsitePages(string app, string website) =>
        Assert.Equal(website, WebsiteHosts.OriginFor(new Uri(app)));

    [Theory]
    [InlineData("http://localhost:6700/open/week")]
    [InlineData("https://media.baptist.com.au/")]
    public void AHostThatIsntAnAppHostHasNoWebsite(string app) =>
        Assert.Null(WebsiteHosts.OriginFor(new Uri(app)));
}
