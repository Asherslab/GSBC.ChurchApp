using GSBC.ChurchApp.Platform.Api.Email;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class ReservedAddressTests
{
    [Fact]
    public void AReservedTestAddressIsNeverSent()
    {
        Assert.True(ReservedAddresses.IsReserved("e2e+places@example.com"));
        Assert.True(ReservedAddresses.IsReserved("tester@phone.test"));
        Assert.True(ReservedAddresses.IsReserved("someone@Mail.Example.ORG."));
        Assert.False(ReservedAddresses.IsReserved("ada@church.org.au"));
        Assert.False(ReservedAddresses.IsReserved("ada@example.com.au"));
    }
}
