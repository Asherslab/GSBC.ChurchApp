using Grpc.Core;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class OriginalRemovalTests
{
    [Fact]
    public void AnOriginalOnlyGoesWhenASizeForPhonesCanTakeItsPlaceOnFullWidthScreens()
    {
        MediaRendition forPhones = Size(MediaSizeName.ForPhones, 1280), thumbnail = Size(MediaSizeName.Thumbnail, 480);

        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaLibrary.CheckOriginalCanGo([])).StatusCode);
        Assert.Equal(StatusCode.FailedPrecondition, Assert.Throws<RpcException>(() => MediaLibrary.CheckOriginalCanGo([thumbnail])).StatusCode);
        MediaLibrary.CheckOriginalCanGo([forPhones, thumbnail]);
    }

    private static MediaRendition Size(MediaSizeName name, int width) =>
        new() { Size = name, Key = $"{name}.jpg", Sha256 = new string('a', 64), ContentType = "image/jpeg", Width = width, Height = width * 9 / 16 };
}
