namespace GSBC.ChurchApp.Features.Accounts.Gateway.Tests;

public sealed class AllowedOriginTests
{
    private static readonly string[] Allowed = ["https://localhost"];

    [Fact]
    public void ACeremonyIsRefusedUnlessItComesFromAnAllowedOrigin()
    {
        Assert.True(PasskeyEndpoints.IsAllowedOrigin("https://localhost", Allowed));
        Assert.True(PasskeyEndpoints.IsAllowedOrigin("HTTPS://LOCALHOST", Allowed));
        Assert.False(PasskeyEndpoints.IsAllowedOrigin(null, Allowed));
        Assert.False(PasskeyEndpoints.IsAllowedOrigin("https://elsewhere.example", Allowed));
    }
}
