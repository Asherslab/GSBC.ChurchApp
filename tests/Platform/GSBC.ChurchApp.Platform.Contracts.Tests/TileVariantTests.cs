using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class TileVariantTests
{
    private static readonly HubTileKind Plain = HubSurfaces.PlainTile;
    private static readonly MediaRef Photo = new() { Id = "p", Key = "public/p.jpg", ContentType = "image/jpeg" };

    [Theory]
    [InlineData(1, 1, false, TileVariants.WordsKey)]
    [InlineData(2, 2, false, TileVariants.WordsKey)]
    [InlineData(1, 1, true, TileVariants.PictureBehind)]
    [InlineData(2, 1, true, TileVariants.PictureAbove)]
    [InlineData(1, 2, true, TileVariants.PictureAbove)]
    public void EachSizeGetsADesignedDefaultAndAPictureChangesIt(int columns, int rows, bool picture, string expected)
    {
        TileLook look = Look(columns, rows, picture, TileVariant.Automatic);

        Assert.Equal(expected, Plain.VariantFor(look, gridColumns: 2).Key);
    }

    [Fact]
    public void AChosenLayoutHoldsWhileItSuitsTheSizeAndFallsBackToTheDefaultWhenItDoesNot()
    {
        Assert.Equal(TileVariants.PictureBeside, Plain.VariantFor(Look(2, 1, true, TileVariants.PictureBeside), 2).Key);
        Assert.Equal(TileVariants.PictureBehind, Plain.VariantFor(Look(1, 1, true, TileVariants.PictureBeside), 2).Key);
        Assert.Equal(TileVariants.PictureBehind, Plain.VariantFor(Look(3, 1, true, TileVariants.PictureBeside), 1).Key);
        Assert.Equal(TileVariants.PictureBehind, Plain.VariantFor(Look(1, 1, true, "gone"), 2).Key);
    }

    [Fact]
    public void EveryKindsLayoutChoiceIsStoredAsAKnownLayoutOrAutomatic()
    {
        Assert.Null(Plain.Tile.Layout.ProblemWith(TileVariants.PictureBeside));
        Assert.Null(Plain.Tile.Layout.ProblemWith(TileVariant.Automatic));
        Assert.NotNull(Plain.Tile.Layout.ProblemWith("poster"));
    }

    private static TileLook Look(int columns, int rows, bool picture, string layout) =>
        new(columns, rows, TileTones.Sand, "", IconTones.Auto, "Camp", "", picture ? Photo : null, FocalPoint.Centre, "", layout);
}
