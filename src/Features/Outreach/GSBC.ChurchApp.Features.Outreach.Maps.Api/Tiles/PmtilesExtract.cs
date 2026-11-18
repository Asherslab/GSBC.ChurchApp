using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;

internal interface IRangeReader
{
    Task<byte[]> ReadAsync(long offset, long length, CancellationToken cancellationToken);
}

internal sealed record ExtractedTiles(long Bytes, int Tiles, int MaxZoom);

internal static class PmtilesExtract
{
    private const int FirstRead = 16_384;
    private const int RootLimit = 16_384 - PmtilesHeader.Length;
    private const long ReadGap = 512 * 1024;
    private const long ReadLimit = 16 * 1024 * 1024;

    public static async Task<ExtractedTiles> ExtractAsync(IRangeReader source, GeoBounds bounds, int maxZoom, Stream output, CancellationToken cancellationToken, int minZoom = 0)
    {
        byte[] start = await source.ReadAsync(0, FirstRead, cancellationToken);
        PmtilesHeader header = PmtilesHeader.Read(start);
        int zoom = Math.Min(maxZoom, header.MaxZoom);
        int lowest = Math.Min(zoom, Math.Max(minZoom, header.MinZoom));
        ulong[] wanted = TileIds.Covering(bounds, zoom, lowest).Order().ToArray();
        List<(ulong Id, long Offset, int Length)> found = [];
        List<PmtilesEntry> root = PmtilesDirectory.Decode(start[(int)header.RootOffset..(int)(header.RootOffset + header.RootLength)], header.InternalCompression);
        await FindAsync(source, header, root, wanted, found, cancellationToken);

        Dictionary<long, long> placed = [];
        List<(long SourceOffset, int Length)> contents = [];
        List<PmtilesEntry> entries = [];
        long next = 0;
        foreach ((ulong id, long offset, int length) in found)
        {
            if (!placed.TryGetValue(offset, out long at))
            {
                placed[offset] = at = next;
                contents.Add((offset, length));
                next += length;
            }
            if (entries.Count > 0 && entries[^1] is var last && last.Offset == at && last.TileId + (ulong)last.RunLength == id)
                entries[^1] = last with { RunLength = last.RunLength + 1 };
            else
                entries.Add(new PmtilesEntry(id, at, length, 1));
        }

        (byte[] rootBytes, byte[] leaves) = Directories(entries);
        byte[] metadata = await source.ReadAsync(header.MetadataOffset, header.MetadataLength, cancellationToken);
        long rootOffset = PmtilesHeader.Length;
        long metadataOffset = rootOffset + rootBytes.Length;
        long leavesOffset = metadataOffset + metadata.Length;
        long dataOffset = leavesOffset + leaves.Length;
        PmtilesHeader written = header with
        {
            RootOffset = rootOffset,
            RootLength = rootBytes.Length,
            MetadataOffset = metadataOffset,
            MetadataLength = metadata.Length,
            LeavesOffset = leavesOffset,
            LeavesLength = leaves.Length,
            TileDataOffset = dataOffset,
            TileDataLength = next,
            AddressedTiles = found.Count,
            TileEntries = entries.Count,
            TileContents = contents.Count,
            Clustered = true,
            InternalCompression = PmtilesCompression.Gzip,
            MinZoom = (byte)lowest,
            MaxZoom = (byte)zoom,
            MinLongitudeE7 = E7(bounds.West),
            MinLatitudeE7 = E7(bounds.South),
            MaxLongitudeE7 = E7(bounds.East),
            MaxLatitudeE7 = E7(bounds.North),
            CentreZoom = (byte)Math.Min(zoom, 12),
            CentreLongitudeE7 = E7(bounds.Centre.Lon),
            CentreLatitudeE7 = E7(bounds.Centre.Lat),
        };
        await output.WriteAsync(written.ToBytes(), cancellationToken);
        await output.WriteAsync(rootBytes, cancellationToken);
        await output.WriteAsync(metadata, cancellationToken);
        await output.WriteAsync(leaves, cancellationToken);
        await CopyContentsAsync(source, header.TileDataOffset, contents, output, cancellationToken);
        return new ExtractedTiles(dataOffset + next, found.Count, zoom);
    }

    private static async Task FindAsync(IRangeReader source, PmtilesHeader header, List<PmtilesEntry> directory, ulong[] wanted,
        List<(ulong Id, long Offset, int Length)> found, CancellationToken cancellationToken)
    {
        Dictionary<int, List<ulong>> byLeaf = [];
        foreach (ulong id in wanted)
        {
            int index = LastAtOrBefore(directory, id);
            if (index < 0)
                continue;
            PmtilesEntry entry = directory[index];
            if (entry.IsLeaf)
            {
                if (!byLeaf.TryGetValue(index, out List<ulong>? ids))
                    byLeaf[index] = ids = [];
                ids.Add(id);
            }
            else if (id < entry.TileId + (ulong)entry.RunLength)
                found.Add((id, entry.Offset, entry.Length));
        }
        foreach ((int index, List<ulong> ids) in byLeaf.OrderBy(pair => pair.Key))
        {
            PmtilesEntry leaf = directory[index];
            byte[] bytes = await source.ReadAsync(header.LeavesOffset + leaf.Offset, leaf.Length, cancellationToken);
            await FindAsync(source, header, PmtilesDirectory.Decode(bytes, header.InternalCompression), [.. ids], found, cancellationToken);
        }
        found.Sort((a, b) => a.Id.CompareTo(b.Id));
    }

    private static int LastAtOrBefore(List<PmtilesEntry> directory, ulong id)
    {
        int low = 0;
        int high = directory.Count - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            if (directory[middle].TileId <= id)
            {
                result = middle;
                low = middle + 1;
            }
            else
                high = middle - 1;
        }
        return result;
    }

    private static (byte[] Root, byte[] Leaves) Directories(List<PmtilesEntry> entries)
    {
        byte[] whole = PmtilesDirectory.Encode(entries);
        if (whole.Length <= RootLimit)
            return (whole, []);
        for (int leafSize = 4096; ; leafSize *= 2)
        {
            using MemoryStream leaves = new();
            List<PmtilesEntry> root = [];
            for (int start = 0; start < entries.Count; start += leafSize)
            {
                List<PmtilesEntry> chunk = entries.GetRange(start, Math.Min(leafSize, entries.Count - start));
                byte[] leaf = PmtilesDirectory.Encode(chunk);
                root.Add(new PmtilesEntry(chunk[0].TileId, leaves.Length, leaf.Length, 0));
                leaves.Write(leaf);
            }
            byte[] rootBytes = PmtilesDirectory.Encode(root);
            if (rootBytes.Length <= RootLimit)
                return (rootBytes, leaves.ToArray());
        }
    }

    private static async Task CopyContentsAsync(IRangeReader source, long dataOffset, List<(long SourceOffset, int Length)> contents, Stream output, CancellationToken cancellationToken)
    {
        int first = 0;
        while (first < contents.Count)
        {
            int last = first;
            long from = contents[first].SourceOffset;
            long to = from + contents[first].Length;
            while (last + 1 < contents.Count && contents[last + 1].SourceOffset >= to && contents[last + 1].SourceOffset - to <= ReadGap
                   && contents[last + 1].SourceOffset + contents[last + 1].Length - from <= ReadLimit)
            {
                last++;
                to = contents[last].SourceOffset + contents[last].Length;
            }
            byte[] chunk = await source.ReadAsync(dataOffset + from, to - from, cancellationToken);
            for (int i = first; i <= last; i++)
                await output.WriteAsync(chunk.AsMemory((int)(contents[i].SourceOffset - from), contents[i].Length), cancellationToken);
            first = last + 1;
        }
    }

    private static int E7(double degrees) => (int)Math.Round(degrees * 10_000_000);
}
