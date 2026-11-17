using GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

public sealed class PmtilesExtractTests
{
    private const int SourceMaxZoom = 6;

    [Theory]
    [InlineData(0, 0u, 0u, 0UL)]
    [InlineData(1, 0u, 0u, 1UL)]
    [InlineData(1, 0u, 1u, 2UL)]
    [InlineData(1, 1u, 1u, 3UL)]
    [InlineData(1, 1u, 0u, 4UL)]
    [InlineData(2, 0u, 0u, 5UL)]
    public void TileIdsFollowThePmtilesHilbertOrder(int zoom, uint x, uint y, ulong id) => Assert.Equal(id, TileIds.Of(zoom, x, y));

    [Fact]
    public async Task ARegionsMapHoldsExactlyTheTilesOverItsBoundsWithTheirOwnContents()
    {
        GeoBounds bounds = new(-27.6, 152.7, -27.1, 153.2);
        MemoryStream output = new();

        ExtractedTiles extracted = await PmtilesExtract.ExtractAsync(new BytesReader(TileArchives.World(SourceMaxZoom)), bounds, 15, output, TestContext.Current.CancellationToken);

        byte[] archive = output.ToArray();
        PmtilesHeader header = PmtilesHeader.Read(archive);
        Dictionary<ulong, byte[]> tiles = Tiles(archive, header);
        Assert.Equal(SourceMaxZoom, extracted.MaxZoom);
        Assert.Equal(archive.Length, extracted.Bytes);
        Assert.Equal(TileIds.Covering(bounds, SourceMaxZoom).Order(), tiles.Keys.Order());
        Assert.All(tiles, tile => Assert.Equal(TileArchives.ContentOf(tile.Key), tile.Value));
    }

    [Fact]
    public async Task APlacesMapLeavesOutTheZoomsBelowItsLowest()
    {
        GeoBounds bounds = new(-27.36, 152.95, -27.33, 152.98);
        MemoryStream output = new();

        await PmtilesExtract.ExtractAsync(new BytesReader(TileArchives.World(SourceMaxZoom)), bounds, 15, output, TestContext.Current.CancellationToken, minZoom: 4);

        byte[] archive = output.ToArray();
        PmtilesHeader header = PmtilesHeader.Read(archive);
        Assert.Equal(4, header.MinZoom);
        Assert.Equal(TileIds.Covering(bounds, SourceMaxZoom, 4).Order(), Tiles(archive, header).Keys.Order());
    }

    private static Dictionary<ulong, byte[]> Tiles(byte[] archive, PmtilesHeader header)
    {
        Dictionary<ulong, byte[]> tiles = [];
        Read(archive[(int)header.RootOffset..(int)(header.RootOffset + header.RootLength)]);
        return tiles;

        void Read(byte[] directory)
        {
            foreach (PmtilesEntry entry in PmtilesDirectory.Decode(directory, header.InternalCompression))
            {
                if (entry.IsLeaf)
                {
                    long start = header.LeavesOffset + entry.Offset;
                    Read(archive[(int)start..(int)(start + entry.Length)]);
                    continue;
                }
                long at = header.TileDataOffset + entry.Offset;
                for (int i = 0; i < entry.RunLength; i++)
                    tiles[entry.TileId + (ulong)i] = archive[(int)at..(int)(at + entry.Length)];
            }
        }
    }
}
