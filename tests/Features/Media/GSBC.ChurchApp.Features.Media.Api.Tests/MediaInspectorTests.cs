namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class MediaInspectorTests
{
    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BytesThatAreNotTheTypeTheyClaimAreRefused()
    {
        Assert.False((await InspectAsync("<html><script>alert(1)</script></html>"u8.ToArray(), "image/png")).MatchesType);
        Assert.True((await InspectAsync(SamplePictures.Jpeg(seed: 1), "image/jpeg")).MatchesType);
    }

    [Fact]
    public async Task ImageDimensionsComeFromTheBytesTurnedTheWayThePhotoIsShown()
    {
        InspectedContent png = await InspectAsync(SamplePictures.Png(width: 640, height: 480), "image/png");
        InspectedContent sidewaysPhoto = await InspectAsync(SamplePictures.JpegWithOrientation(width: 4000, height: 3000, orientation: 6), "image/jpeg");

        Assert.Equal(new Dimensions(640, 480), png.Dimensions);
        Assert.Equal(new Dimensions(3000, 4000), sidewaysPhoto.Dimensions);
    }

    private static async Task<InspectedContent> InspectAsync(byte[] bytes, string contentType)
    {
        using MemoryStream content = new(bytes);
        return await MediaInspector.InspectAsync(content, contentType, Cancellation);
    }
}
