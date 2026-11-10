using GSBC.ChurchApp.Features.Accounts.Data;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

public sealed class SessionDevicesTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.3 Safari/605.1.15", "Safari on a Mac")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36", "Chrome on a Mac")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/156.0.0.0 Safari/537.36 Edg/156.0.0.0", "Edge on Windows")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 26_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/156.0 Mobile/15E148 Safari/604.1", "Chrome on an iPhone")]
    [InlineData("Mozilla/5.0 (Android 16; Mobile; rv:150.0) Gecko/150.0 Firefox/150.0", "Firefox on Android")]
    [InlineData("curl/8.7.1", "A browser")]
    public void ABrowserIsNamedByItsBrowserAndSystemInsteadOfItsUserAgent(string userAgent, string expected) =>
        Assert.Equal(expected, SessionDevices.Describe(SessionClient.Browser, userAgent));

    [Fact]
    public void AnAppOrToolKeepsTheNameItGaveItself()
    {
        Assert.Equal("Sam's iPhone", SessionDevices.Describe(SessionClient.App, "Sam's iPhone"));
        Assert.Equal("Claude on my Mac", SessionDevices.Describe(SessionClient.Tool, "Claude on my Mac"));
    }
}
