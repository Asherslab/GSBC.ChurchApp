using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

public sealed class ReviewCodeTests
{
    private static readonly PermissionCatalog Catalog = new([new PermissionSet(AccountsPermissions.All)]);

    [Fact]
    public void CodesAreGroupedAndFreeOfLookalikes()
    {
        string code = ReviewCodes.NewCode();

        Assert.Matches("^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$", code);
        Assert.DoesNotContain(code, character => "01258BILOSZ".Contains(character));
    }

    [Fact]
    public void ACodeMatchesHoweverItIsTypedAndSignsInUntilItExpiresOrIsTurnedOff()
    {
        string code = ReviewCodes.NewCode();
        ReviewCode stored = new()
        {
            Hash = ReviewCodes.HashOf(code), Label = "TestFlight", CreatedAt = TestClock.Start, ExpiresAt = TestClock.Start.AddDays(30),
        };

        Assert.Equal(stored.Hash, ReviewCodes.HashOf(code.ToLowerInvariant().Replace("-", " ")));
        Assert.True(stored.IsLive(TestClock.Start));
        Assert.False(stored.IsLive(stored.ExpiresAt));
        stored.TurnedOffAt = TestClock.Start;
        Assert.False(stored.IsLive(TestClock.Start));
    }

    [Fact]
    public void ACodeNeverReachesAnAccountThatIsNotADemoAccount()
    {
        AccountUser account = new() { Id = Guid.CreateVersion7(), DisplayName = "Demo" };
        GrantSet everything = new([new Grant(PermissionPattern.Parse("*"), Scope.Global)]);
        GrantSet everythingButSensitive = new([.. Catalog.ByNode.Values.Select(permission => new Grant(PermissionPattern.Parse($"-{permission.Node}"), Scope.Global))]);

        Assert.Null(DemoAccounts.WhyCodesCannotSignIn(account, isDemo: true, hasPasskey: false, GrantSet.Empty, Catalog));
        Assert.Null(DemoAccounts.WhyCodesCannotSignIn(account, isDemo: true, hasPasskey: false, everythingButSensitive, Catalog));
        Assert.NotNull(DemoAccounts.WhyCodesCannotSignIn(account, isDemo: false, hasPasskey: false, GrantSet.Empty, Catalog));
        Assert.NotNull(DemoAccounts.WhyCodesCannotSignIn(account, isDemo: true, hasPasskey: true, GrantSet.Empty, Catalog));
        Assert.NotNull(DemoAccounts.WhyCodesCannotSignIn(account, isDemo: true, hasPasskey: false, everything, Catalog));

        account.Disabled = true;
        Assert.NotNull(DemoAccounts.WhyCodesCannotSignIn(account, isDemo: true, hasPasskey: false, GrantSet.Empty, Catalog));
    }
}
