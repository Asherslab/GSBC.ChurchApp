using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class OriginalRemovalTests
{
    [Fact]
    public void AnOriginalOnlyGoesWhenASizeForPhonesCanTakeItsPlaceOnFullWidthScreens()
    {
        MediaRendition forPhones = Size(MediaSizeName.ForPhones, 1280), thumbnail = Size(MediaSizeName.Thumbnail, 480);

        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaLibrary.CheckOriginalCanGo([], everyUseCanSwitch: true)).StatusCode);
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaLibrary.CheckOriginalCanGo([thumbnail], everyUseCanSwitch: true)).StatusCode);
        MediaLibrary.CheckOriginalCanGo([forPhones, thumbnail], everyUseCanSwitch: true);
    }

    [Fact]
    public void AnOriginalStaysWhileSomewhereUsingItCanNotSwitchToASmallerSizeByItself()
    {
        IMediaHolder[] holders = [new Tiles()];

        Assert.True(MediaHolders.CanAllRefresh(holders, ["tile/1", "tile/2"]));
        Assert.False(MediaHolders.CanAllRefresh(holders, ["tile/1", "draft/1"]));
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() =>
            MediaLibrary.CheckOriginalCanGo([Size(MediaSizeName.ForPhones, 1280)], everyUseCanSwitch: false)).StatusCode);
    }

    private sealed class Tiles : IMediaHolder
    {
        public bool Holds(string owner) => owner.StartsWith("tile/", StringComparison.Ordinal);

        public Task RefreshAsync(string owner, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static MediaRendition Size(MediaSizeName name, int width) =>
        new() { Size = name, Key = $"{name}.jpg", Sha256 = new string('a', 64), ContentType = "image/jpeg", Width = width, Height = width * 9 / 16 };
}
