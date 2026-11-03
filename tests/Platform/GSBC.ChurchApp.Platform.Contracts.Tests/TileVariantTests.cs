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

    [Fact]
    public void ASavedTileKeepsEveryValueExactlyEvenWithSymbolsAndPictures()
    {
        Dictionary<string, string> tile = new()
        {
            [HubTileKeys.Title] = "Give & grow = 100% joy",
            [HubTileKeys.Link] = "https://baptist.com.au/give?campaign=a&b=c",
            [HubTileKeys.Image] = """{"id":"p","key":"public/p.jpg"}""",
            [HubTileKeys.Columns] = "2",
        };

        string stored = HubSurfaces.Saved.Tile.Format(tile);

        Assert.Null(HubSurfaces.Saved.Tile.ProblemWith(stored));
        Assert.True(HubSurfaces.Saved.Tile.TryParse(stored, out IReadOnlyDictionary<string, string> back));
        Assert.Equal(tile.OrderBy(pair => pair.Key), back.OrderBy(pair => pair.Key));
        Assert.NotNull(HubSurfaces.Saved.Kind.ProblemWith("<script>"));
    }

    private static TileLook Look(int columns, int rows, bool picture, string layout) =>
        new(columns, rows, TileTones.Sand, "", IconTones.Auto, "Camp", "", picture ? Photo : null, FocalPoint.Centre, "", layout);
}
