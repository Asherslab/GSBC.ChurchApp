using GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tests;

internal static class TileArchives
{
    public static byte[] ContentOf(ulong id) => BitConverter.GetBytes(id % 7 == 0 ? 7 : id);

    public static byte[] World(int maxZoom)
    {
        ulong count = ((1UL << (2 * (maxZoom + 1))) - 1) / 3;
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
            MaxZoom = (byte)maxZoom,
        };
        return [.. header.ToBytes(), .. root, .. metadata, .. data.ToArray()];
    }
}

internal sealed class BytesReader(byte[] bytes) : IRangeReader
{
    public Task<byte[]> ReadAsync(long offset, long length, CancellationToken cancellationToken) =>
        Task.FromResult(bytes[(int)offset..(int)Math.Min(bytes.Length, offset + length)]);
}
