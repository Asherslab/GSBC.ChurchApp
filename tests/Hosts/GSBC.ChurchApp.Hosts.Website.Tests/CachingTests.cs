namespace GSBC.ChurchApp.Hosts.Website.Tests;

public sealed class CachingTests
{
    private const string Fingerprinted = "max-age=31536000, immutable";

    [Fact]
    public void DevelopmentStoresNothing()
    {
        Assert.Equal("no-store", Caching.For(development: true, 200, null));
        Assert.Equal("no-store", Caching.For(development: true, 200, Fingerprinted));
    }

    [Fact]
    public void PagesAreKeptAMinuteAndMissingPagesAreAskedForAgain()
    {
        Assert.Equal("public, max-age=60", Caching.For(development: false, 200, null));
        Assert.Equal("no-cache", Caching.For(development: false, 404, null));
        Assert.Equal("no-cache", Caching.For(development: false, 503, null));
    }

    [Fact]
    public void StaticFilesKeepTheirOwnCaching() =>
        Assert.Equal(Fingerprinted, Caching.For(development: false, 200, Fingerprinted));

    [Fact]
    public void OnlyTheNamedWebsitesMayFrameThePages()
    {
        Assert.Equal("frame-ancestors https://church.example https://www.church.example", Caching.FramedBy("https://church.example https://www.church.example"));
        Assert.Equal("frame-ancestors 'none'", Caching.FramedBy(""));
    }
}
