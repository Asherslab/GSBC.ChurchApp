using System.Buffers.Binary;
using System.IO.Compression;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Tiles;

internal enum PmtilesCompression : byte
{
    None = 1,
    Gzip = 2,
}

internal sealed record PmtilesHeader
{
    public const int Length = 127;
    private static ReadOnlySpan<byte> Magic => "PMTiles"u8;
    private const byte Version = 3;

    public long RootOffset { get; init; }
    public long RootLength { get; init; }
    public long MetadataOffset { get; init; }
    public long MetadataLength { get; init; }
    public long LeavesOffset { get; init; }
    public long LeavesLength { get; init; }
    public long TileDataOffset { get; init; }
    public long TileDataLength { get; init; }
    public long AddressedTiles { get; init; }
    public long TileEntries { get; init; }
    public long TileContents { get; init; }
    public bool Clustered { get; init; }
    public PmtilesCompression InternalCompression { get; init; }
    public PmtilesCompression TileCompression { get; init; }
    public byte TileType { get; init; }
    public byte MinZoom { get; init; }
    public byte MaxZoom { get; init; }
    public int MinLongitudeE7 { get; init; }
    public int MinLatitudeE7 { get; init; }
    public int MaxLongitudeE7 { get; init; }
    public int MaxLatitudeE7 { get; init; }
    public byte CentreZoom { get; init; }
    public int CentreLongitudeE7 { get; init; }
    public int CentreLatitudeE7 { get; init; }

    public static PmtilesHeader Read(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < Length || !bytes[..7].SequenceEqual(Magic) || bytes[7] != Version)
            throw new InvalidDataException("Not a version 3 PMTiles archive");
        return new PmtilesHeader
        {
            RootOffset = Long(bytes, 8),
            RootLength = Long(bytes, 16),
            MetadataOffset = Long(bytes, 24),
            MetadataLength = Long(bytes, 32),
            LeavesOffset = Long(bytes, 40),
            LeavesLength = Long(bytes, 48),
            TileDataOffset = Long(bytes, 56),
            TileDataLength = Long(bytes, 64),
            AddressedTiles = Long(bytes, 72),
            TileEntries = Long(bytes, 80),
            TileContents = Long(bytes, 88),
            Clustered = bytes[96] == 1,
            InternalCompression = (PmtilesCompression)bytes[97],
            TileCompression = (PmtilesCompression)bytes[98],
            TileType = bytes[99],
            MinZoom = bytes[100],
            MaxZoom = bytes[101],
            MinLongitudeE7 = BinaryPrimitives.ReadInt32LittleEndian(bytes[102..]),
            MinLatitudeE7 = BinaryPrimitives.ReadInt32LittleEndian(bytes[106..]),
            MaxLongitudeE7 = BinaryPrimitives.ReadInt32LittleEndian(bytes[110..]),
            MaxLatitudeE7 = BinaryPrimitives.ReadInt32LittleEndian(bytes[114..]),
            CentreZoom = bytes[118],
            CentreLongitudeE7 = BinaryPrimitives.ReadInt32LittleEndian(bytes[119..]),
            CentreLatitudeE7 = BinaryPrimitives.ReadInt32LittleEndian(bytes[123..]),
        };
    }

    public byte[] ToBytes()
    {
        byte[] bytes = new byte[Length];
        Magic.CopyTo(bytes);
        bytes[7] = Version;
        Span<byte> span = bytes;
        long[] longs = [RootOffset, RootLength, MetadataOffset, MetadataLength, LeavesOffset, LeavesLength, TileDataOffset, TileDataLength, AddressedTiles, TileEntries, TileContents];
        for (int i = 0; i < longs.Length; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(span[(8 + 8 * i)..], (ulong)longs[i]);
        bytes[96] = Clustered ? (byte)1 : (byte)0;
        bytes[97] = (byte)InternalCompression;
        bytes[98] = (byte)TileCompression;
        bytes[99] = TileType;
        bytes[100] = MinZoom;
        bytes[101] = MaxZoom;
        BinaryPrimitives.WriteInt32LittleEndian(span[102..], MinLongitudeE7);
        BinaryPrimitives.WriteInt32LittleEndian(span[106..], MinLatitudeE7);
        BinaryPrimitives.WriteInt32LittleEndian(span[110..], MaxLongitudeE7);
        BinaryPrimitives.WriteInt32LittleEndian(span[114..], MaxLatitudeE7);
        bytes[118] = CentreZoom;
        BinaryPrimitives.WriteInt32LittleEndian(span[119..], CentreLongitudeE7);
        BinaryPrimitives.WriteInt32LittleEndian(span[123..], CentreLatitudeE7);
        return bytes;
    }

    private static long Long(ReadOnlySpan<byte> bytes, int at) => (long)BinaryPrimitives.ReadUInt64LittleEndian(bytes[at..]);
}

internal readonly record struct PmtilesEntry(ulong TileId, long Offset, int Length, int RunLength)
{
    public bool IsLeaf => RunLength == 0;
}

internal static class PmtilesDirectory
{
    public static List<PmtilesEntry> Decode(byte[] compressed, PmtilesCompression compression)
    {
        byte[] bytes = Decompress(compressed, compression);
        int at = 0;
        int count = (int)Varint(bytes, ref at);
        ulong[] ids = new ulong[count];
        int[] runs = new int[count];
        int[] lengths = new int[count];
        long[] offsets = new long[count];
        ulong last = 0;
        for (int i = 0; i < count; i++)
            ids[i] = last += Varint(bytes, ref at);
        for (int i = 0; i < count; i++)
            runs[i] = (int)Varint(bytes, ref at);
        for (int i = 0; i < count; i++)
            lengths[i] = (int)Varint(bytes, ref at);
        for (int i = 0; i < count; i++)
        {
            ulong value = Varint(bytes, ref at);
            offsets[i] = value == 0 && i > 0 ? offsets[i - 1] + lengths[i - 1] : (long)value - 1;
        }
        List<PmtilesEntry> entries = new(count);
        for (int i = 0; i < count; i++)
            entries.Add(new PmtilesEntry(ids[i], offsets[i], lengths[i], runs[i]));
        return entries;
    }

    public static byte[] Encode(IReadOnlyList<PmtilesEntry> entries)
    {
        using MemoryStream raw = new();
        WriteVarint(raw, (ulong)entries.Count);
        ulong last = 0;
        foreach (PmtilesEntry entry in entries)
        {
            WriteVarint(raw, entry.TileId - last);
            last = entry.TileId;
        }
        foreach (PmtilesEntry entry in entries)
            WriteVarint(raw, (ulong)entry.RunLength);
        foreach (PmtilesEntry entry in entries)
            WriteVarint(raw, (ulong)entry.Length);
        for (int i = 0; i < entries.Count; i++)
        {
            bool follows = i > 0 && entries[i].Offset == entries[i - 1].Offset + entries[i - 1].Length;
            WriteVarint(raw, follows ? 0 : (ulong)entries[i].Offset + 1);
        }
        return Gzip(raw.ToArray());
    }

    public static byte[] Gzip(byte[] bytes)
    {
        using MemoryStream compressed = new();
        using (GZipStream gzip = new(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            gzip.Write(bytes);
        return compressed.ToArray();
    }

    private static byte[] Decompress(byte[] bytes, PmtilesCompression compression)
    {
        if (compression == PmtilesCompression.None)
            return bytes;
        if (compression != PmtilesCompression.Gzip)
            throw new InvalidDataException($"PMTiles compression {compression} isn't supported");
        using GZipStream gzip = new(new MemoryStream(bytes), CompressionMode.Decompress);
        using MemoryStream raw = new();
        gzip.CopyTo(raw);
        return raw.ToArray();
    }

    private static ulong Varint(byte[] bytes, ref int at)
    {
        ulong value = 0;
        for (int shift = 0; ; shift += 7)
        {
            byte next = bytes[at++];
            value |= (ulong)(next & 0x7f) << shift;
            if (next < 0x80)
                return value;
        }
    }

    private static void WriteVarint(Stream stream, ulong value)
    {
        while (value >= 0x80)
        {
            stream.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        stream.WriteByte((byte)value);
    }
}
