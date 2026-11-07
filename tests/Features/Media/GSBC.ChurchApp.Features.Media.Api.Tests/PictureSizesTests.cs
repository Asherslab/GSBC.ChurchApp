using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using SkiaSharp;

namespace GSBC.ChurchApp.Features.Media.Api.Tests;

public sealed class PictureSizesTests
{
    [Fact]
    public void APictureGetsASizeForPhonesAndAThumbnailOnlyWhereItIsWiderThanThem()
    {
        PictureSizesMade large = PictureSizes.Make(SamplePictures.Picture(3000, 1688), "image/jpeg");
        PictureSizesMade small = PictureSizes.Make(SamplePictures.Picture(400, 300), "image/jpeg");

        Assert.Equal(PictureSizing.Made, large.Sizing);
        Assert.Equal([(MediaSizeName.ForPhones, 1280, 720), (MediaSizeName.Thumbnail, 480, 270)], large.Sizes.Select(size => (size.Name, size.Width, size.Height)));
        Assert.Equal(PictureSizing.Made, small.Sizing);
        Assert.Empty(small.Sizes);
    }

    [Fact]
    public void SizesShowTheWholePictureTurnedTheWayItIsShownAndNeverCropIt()
    {
        foreach (SizedPicture size in PictureSizes.Make(SamplePictures.Picture(3000, 1688), "image/jpeg").Sizes)
        {
            using SKBitmap shown = SKBitmap.Decode(size.Bytes);
            Assert.Equal((size.Width, size.Height), (shown.Width, shown.Height));
            Assert.Equal(3000d / 1688, (double)shown.Width / shown.Height, 0.01);
            Assert.Equal(SamplePictures.Corners, SamplePictures.CornersOf(shown));
        }

        SizedPicture thumbnail = Assert.Single(PictureSizes.Make(SamplePictures.WithOrientation(SamplePictures.Picture(2000, 1000), orientation: 6), "image/jpeg").Sizes);
        Assert.Equal((MediaSizeName.Thumbnail, 480, 960), (thumbnail.Name, thumbnail.Width, thumbnail.Height));
        using SKBitmap turned = SKBitmap.Decode(thumbnail.Bytes);
        SKColor[] corners = SamplePictures.Corners;
        Assert.Equal([corners[2], corners[0], corners[3], corners[1]], SamplePictures.CornersOf(turned));
    }
}
