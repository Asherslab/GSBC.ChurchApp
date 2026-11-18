using Grpc.Core;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class GrantedAccessTests
{
    private static readonly Permission WriteNews = new("news.write", ScopeKind.Global, "Write news");

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Internal)]
    public void ADroppedConnectionKeepsWhatSomeoneMayDoSoTheFormTheyAreTypingInStays(StatusCode status)
    {
        GrantedAccess access = SignedInWriter();
        int changes = 0;
        access.Changed += () => changes++;

        access.CouldNotLoad(status);

        Assert.True(access.SignedIn);
        Assert.True(access.Can(WriteNews));
        Assert.Equal(0, changes);
    }

    [Theory]
    [InlineData(StatusCode.Unauthenticated)]
    [InlineData(StatusCode.PermissionDenied)]
    public void TheServerTurningTheSessionAwaySignsThemOut(StatusCode status)
    {
        GrantedAccess access = SignedInWriter();

        access.CouldNotLoad(status);

        Assert.False(access.SignedIn);
        Assert.False(access.Can(WriteNews));
    }

    private static GrantedAccess SignedInWriter()
    {
        GrantedAccess access = new();
        access.Replace(new GrantSet([new Grant(PermissionPattern.Parse("news.*"), Scope.Global)]), signedIn: true);
        return access;
    }
}
