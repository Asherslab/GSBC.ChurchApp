using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Sources;

internal static class OsmPbf
{
    private const string DataBlock = "OSMData";
    private const double Nano = 1e-9;
    private const string HighwayKey = "highway";

    public static async Task<OsmData> ReadAsync(Stream pbf, GeoBounds area, Func<IReadOnlyDictionary<string, string>, bool> keepWay, CancellationToken cancellationToken)
    {
        SortedNodes nodes = new();
        OsmData data = new();
        byte[] lengthBytes = new byte[4];
        while (await ReadExactlyOrEndAsync(pbf, lengthBytes, cancellationToken))
        {
            byte[] header = new byte[BinaryPrimitives.ReadInt32BigEndian(lengthBytes)];
            await pbf.ReadExactlyAsync(header, cancellationToken);
            (string type, int size) = BlobHeader(header);
            byte[] blob = new byte[size];
            await pbf.ReadExactlyAsync(blob, cancellationToken);
            if (type == DataBlock)
                ReadBlock(Inflate(blob), area, keepWay, nodes, data);
        }
        return data;
    }

    private static void ReadBlock(byte[] block, GeoBounds area, Func<IReadOnlyDictionary<string, string>, bool> keepWay, SortedNodes nodes, OsmData data)
    {
        List<string> strings = [];
        List<ReadOnlyMemory<byte>> groups = [];
        long granularity = 100, latitudeOffset = 0, longitudeOffset = 0;
        ProtoReader reader = new(block);
        while (reader.Next(out int field, out int wire))
        {
            switch (field)
            {
                case 1:
                    ProtoReader table = new(reader.Bytes());
                    while (table.Next(out int f, out int w))
                        if (f == 1) strings.Add(Encoding.UTF8.GetString(table.Bytes().Span));
                        else table.Skip(w);
                    break;
                case 2: groups.Add(reader.Bytes()); break;
                case 17: granularity = (long)reader.Varint(); break;
                case 19: latitudeOffset = (long)reader.Varint(); break;
                case 20: longitudeOffset = (long)reader.Varint(); break;
                default: reader.Skip(wire); break;
            }
        }
        foreach (ReadOnlyMemory<byte> group in groups)
        {
            ProtoReader groupReader = new(group);
            while (groupReader.Next(out int field, out int wire))
            {
                switch (field)
                {
                    case 2: ReadDenseNodes(groupReader.Bytes(), area, nodes, granularity, latitudeOffset, longitudeOffset); break;
                    case 3: ReadWay(groupReader.Bytes(), strings, keepWay, nodes, data); break;
                    default: groupReader.Skip(wire); break;
                }
            }
        }
    }

    private static void ReadDenseNodes(ReadOnlyMemory<byte> dense, GeoBounds area, SortedNodes nodes, long granularity, long latitudeOffset, long longitudeOffset)
    {
        long[] ids = [], latitudes = [], longitudes = [];
        ProtoReader reader = new(dense);
        while (reader.Next(out int field, out int wire))
        {
            switch (field)
            {
                case 1: ids = reader.PackedSigned(); break;
                case 8: latitudes = reader.PackedSigned(); break;
                case 9: longitudes = reader.PackedSigned(); break;
                default: reader.Skip(wire); break;
            }
        }
        long id = 0, latitude = 0, longitude = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            id += ids[i];
            latitude += latitudes[i];
            longitude += longitudes[i];
            GeoPoint point = new(Nano * (latitudeOffset + granularity * latitude), Nano * (longitudeOffset + granularity * longitude));
            if (area.Contains(point))
                nodes.Add(id, point);
        }
    }

    private static void ReadWay(ReadOnlyMemory<byte> way, List<string> strings, Func<IReadOnlyDictionary<string, string>, bool> keepWay, SortedNodes nodes, OsmData data)
    {
        long id = 0;
        long[] keys = [], values = [], refs = [];
        ProtoReader reader = new(way);
        while (reader.Next(out int field, out int wire))
        {
            switch (field)
            {
                case 1: id = (long)reader.Varint(); break;
                case 2: keys = reader.PackedUnsigned(); break;
                case 3: values = reader.PackedUnsigned(); break;
                case 8: refs = reader.PackedSigned(); break;
                default: reader.Skip(wire); break;
            }
        }
        if (!keys.Any(key => strings[(int)key] == HighwayKey))
            return;
        Dictionary<string, string> tags = [];
        for (int i = 0; i < keys.Length; i++)
            tags[strings[(int)keys[i]]] = strings[(int)values[i]];
        if (!keepWay(tags))
            return;
        List<long> longest = [];
        List<long> run = [];
        long node = 0;
        foreach (long delta in refs)
        {
            node += delta;
            if (nodes.Find(node) is { } point)
            {
                run.Add(node);
                data.Nodes[node] = point;
                continue;
            }
            if (run.Count > longest.Count)
                longest = run;
            run = [];
        }
        if (run.Count > longest.Count)
            longest = run;
        if (longest.Count >= 2)
            data.Ways[id] = new OsmWay(id, [.. longest], tags);
    }

    private static (string Type, int Size) BlobHeader(byte[] header)
    {
        string type = "";
        int size = 0;
        ProtoReader reader = new(header);
        while (reader.Next(out int field, out int wire))
        {
            if (field == 1) type = Encoding.UTF8.GetString(reader.Bytes().Span);
            else if (field == 3) size = (int)reader.Varint();
            else reader.Skip(wire);
        }
        return (type, size);
    }

    private static byte[] Inflate(byte[] blob)
    {
        ProtoReader reader = new(blob);
        while (reader.Next(out int field, out int wire))
        {
            switch (field)
            {
                case 1: return reader.Bytes().ToArray();
                case 3:
                    using (ZLibStream zlib = new(new MemoryStream(reader.Bytes().ToArray()), CompressionMode.Decompress))
                    using (MemoryStream raw = new())
                    {
                        zlib.CopyTo(raw);
                        return raw.ToArray();
                    }
                default: reader.Skip(wire); break;
            }
        }
        throw new InvalidDataException("An OSM PBF blob held neither raw nor zlib data");
    }

    private static async Task<bool> ReadExactlyOrEndAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        int read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
        return read == buffer.Length;
    }

    private sealed class SortedNodes
    {
        private readonly List<long> _ids = [];
        private readonly List<GeoPoint> _points = [];

        public void Add(long id, GeoPoint point)
        {
            _ids.Add(id);
            _points.Add(point);
        }

        public GeoPoint? Find(long id)
        {
            int index = _ids.BinarySearch(id);
            return index >= 0 ? _points[index] : null;
        }
    }

    private struct ProtoReader(ReadOnlyMemory<byte> bytes)
    {
        private int _at;

        public bool Next(out int field, out int wire)
        {
            if (_at >= bytes.Length)
            {
                (field, wire) = (0, 0);
                return false;
            }
            ulong key = Varint();
            (field, wire) = ((int)(key >> 3), (int)(key & 7));
            return true;
        }

        public ulong Varint()
        {
            ReadOnlySpan<byte> span = bytes.Span;
            ulong value = 0;
            for (int shift = 0; ; shift += 7)
            {
                byte next = span[_at++];
                value |= (ulong)(next & 0x7f) << shift;
                if (next < 0x80)
                    return value;
            }
        }

        public ReadOnlyMemory<byte> Bytes()
        {
            int length = (int)Varint();
            ReadOnlyMemory<byte> slice = bytes.Slice(_at, length);
            _at += length;
            return slice;
        }

        public long[] PackedSigned() => Packed(value => (long)(value >> 1) ^ -(long)(value & 1));

        public long[] PackedUnsigned() => Packed(value => (long)value);

        private long[] Packed(Func<ulong, long> decode)
        {
            ProtoReader inner = new(Bytes());
            List<long> values = [];
            while (inner._at < inner.Length)
                values.Add(decode(inner.Varint()));
            return [.. values];
        }

        private int Length => bytes.Length;

        public void Skip(int wire)
        {
            switch (wire)
            {
                case 0: Varint(); break;
                case 1: _at += 8; break;
                case 2: Bytes(); break;
                case 5: _at += 4; break;
                default: throw new InvalidDataException($"Unknown protobuf wire type {wire}");
            }
        }
    }
}
