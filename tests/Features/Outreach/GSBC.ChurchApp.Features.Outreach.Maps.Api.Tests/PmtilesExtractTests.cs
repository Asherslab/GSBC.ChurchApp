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

        ExtractedTiles extracted = await PmtilesExtract.ExtractAsync(new BytesReader(SourceArchive()), bounds, 15, output, TestContext.Current.CancellationToken);

        byte[] archive = output.ToArray();
        PmtilesHeader header = PmtilesHeader.Read(archive);
        Dictionary<ulong, byte[]> tiles = Tiles(archive, header);
        Assert.Equal(SourceMaxZoom, extracted.MaxZoom);
        Assert.Equal(archive.Length, extracted.Bytes);
        Assert.Equal(TileIds.Covering(bounds, SourceMaxZoom).Order(), tiles.Keys.Order());
        Assert.All(tiles, tile => Assert.Equal(ContentOf(tile.Key), tile.Value));
    }

    private static byte[] ContentOf(ulong id) => BitConverter.GetBytes(id % 7 == 0 ? 7 : id);

    private static byte[] SourceArchive()
    {
        ulong count = ((1UL << (2 * (SourceMaxZoom + 1))) - 1) / 3;
        MemoryStream data = new();
        List<PmtilesEntry> entries = [];
        Dictionary<ulong, long> shared = [];
        for (ulong id = 0; id < count; id++)
        {
            byte[] content = ContentOf(id);
            ulong value = BitConverter.ToUInt64(content);
            if (!shared.TryGetValue(value, out long offset))
            {
                shared[value] = offset = data.Length;
                data.Write(content);
            }
            entries.Add(new PmtilesEntry(id, offset, content.Length, 1));
        }
        byte[] root = PmtilesDirectory.Encode(entries);
        byte[] metadata = PmtilesDirectory.Gzip("{}"u8.ToArray());
        PmtilesHeader header = new()
        {
            RootOffset = PmtilesHeader.Length,
            RootLength = root.Length,
            MetadataOffset = PmtilesHeader.Length + root.Length,
            MetadataLength = metadata.Length,
            LeavesOffset = PmtilesHeader.Length + root.Length + metadata.Length,
            TileDataOffset = PmtilesHeader.Length + root.Length + metadata.Length,
            TileDataLength = data.Length,
            InternalCompression = PmtilesCompression.Gzip,
            TileCompression = PmtilesCompression.Gzip,
            TileType = 1,
            MaxZoom = SourceMaxZoom,
        };
        return [.. header.ToBytes(), .. root, .. metadata, .. data.ToArray()];
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

    private sealed class BytesReader(byte[] bytes) : IRangeReader
    {
        public Task<byte[]> ReadAsync(long offset, long length, CancellationToken cancellationToken) =>
            Task.FromResult(bytes[(int)offset..(int)Math.Min(bytes.Length, offset + length)]);
    }
}
