using System.IO.Compression;
using System.Text;

namespace GSBC.ChurchApp.Features.Outreach.Routing;

public static class StreetGraphFile
{
    public const ushort FormatVersion = 1;
    public const string ContentType = "application/vnd.gsbc.streets";
    public const string Extension = ".streets";

    private const int AlongSteps = 10_000;
    private const double DecimetresPerMetre = 10;
    private const byte Plain = 0;
    private const byte Deflated = 1;
    private static readonly byte[] Magic = "GSBCST"u8.ToArray();

    public static double QuantizeAlong(double along) => Math.Round(Math.Clamp(along, 0, 1) * AlongSteps) / AlongSteps;

    public static Flat QuantizeFlat(Flat flat) =>
        new(Math.Round(flat.X * DecimetresPerMetre) / DecimetresPerMetre, Math.Round(flat.Y * DecimetresPerMetre) / DecimetresPerMetre);

    public static void Write(StreetGraph graph, Stream output, bool compress = true)
    {
        output.Write(Magic);
        output.Write(BitConverter.GetBytes(FormatVersion));
        output.WriteByte(compress ? Deflated : Plain);
        if (!compress)
        {
            WritePayload(graph, output);
            return;
        }
        using ZLibStream zlib = new(output, CompressionLevel.SmallestSize, leaveOpen: true);
        WritePayload(graph, zlib);
    }

    public static byte[] ToBytes(StreetGraph graph, bool compress = true)
    {
        using MemoryStream stream = new();
        Write(graph, stream, compress);
        return stream.ToArray();
    }

    public static StreetGraph Read(Stream input)
    {
        Span<byte> header = stackalloc byte[Magic.Length + 3];
        input.ReadExactly(header);
        if (!header[..Magic.Length].SequenceEqual(Magic))
            throw new InvalidDataException("This isn't a street graph file.");
        ushort version = BitConverter.ToUInt16(header.Slice(Magic.Length, 2));
        if (version != FormatVersion)
            throw new InvalidDataException($"Street graph format {version} isn't supported; this app reads format {FormatVersion}.");
        byte[] payload;
        using (MemoryStream buffer = new())
        {
            if (header[^1] == Deflated)
            {
                using ZLibStream zlib = new(input, CompressionMode.Decompress, leaveOpen: true);
                zlib.CopyTo(buffer);
            }
            else
            {
                input.CopyTo(buffer);
            }
            payload = buffer.ToArray();
        }
        return ReadPayload(payload);
    }

    public static StreetGraph Read(byte[] bytes)
    {
        using MemoryStream stream = new(bytes, writable: false);
        return Read(stream);
    }

    private static void WritePayload(StreetGraph graph, Stream output)
    {
        PayloadWriter w = new();
        StreetRegion region = graph.Region;
        w.String(region.Id);
        w.String(region.Name);
        w.String(region.Attribution);
        w.Signed(region.Version);
        w.Signed(region.Built.ToUnixTimeMilliseconds());
        w.Bounds(region.Bounds);
        w.Bounds(region.Core);
        w.Double(region.Origin.Lat);
        w.Double(region.Origin.Lon);

        Dictionary<string, int> strings = [];
        List<string> table = [];
        int StringId(string? value)
        {
            if (value is null)
                return -1;
            if (!strings.TryGetValue(value, out int id))
            {
                id = table.Count;
                strings[value] = id;
                table.Add(value);
            }
            return id;
        }
        foreach (StreetInfo street in graph.Streets)
        {
            StringId(street.Name);
            StringId(street.FullName);
        }
        foreach (string? label in graph.BoxLabels)
            StringId(label);
        w.Unsigned((ulong)table.Count);
        foreach (string value in table)
            w.String(value);

        w.Unsigned((ulong)graph.NodeCount);
        long x = 0, y = 0, osm = 0;
        for (int n = 0; n < graph.NodeCount; n++)
        {
            (long nx, long ny) = Decimetres(graph.NodeFlat(n));
            w.Signed(nx - x);
            w.Signed(ny - y);
            w.Signed(graph.NodeOsm[n] - osm);
            (x, y, osm) = (nx, ny, graph.NodeOsm[n]);
        }

        w.Unsigned((ulong)graph.StreetCount);
        long way = 0;
        for (int s = 0; s < graph.StreetCount; s++)
        {
            StreetInfo street = graph.Streets[s];
            w.Unsigned((ulong)street.U);
            w.Unsigned((ulong)street.V);
            w.Unsigned((ulong)StringId(street.Name));
            w.Unsigned((ulong)StringId(street.FullName));
            w.Byte((byte)street.Kind);
            w.Signed(street.Way - way);
            way = street.Way;
            ReadOnlySpan<Flat> line = graph.Shape(s);
            w.Unsigned((ulong)(line.Length - 2));
            (long px, long py) = Decimetres(line[0]);
            for (int i = 1; i < line.Length - 1; i++)
            {
                (long qx, long qy) = Decimetres(line[i]);
                w.Signed(qx - px);
                w.Signed(qy - py);
                (px, py) = (qx, qy);
            }
        }

        for (int side = 0; side < graph.StreetCount * 2; side++)
        {
            int from = graph.BoxStarts[side], to = graph.BoxStarts[side + 1];
            w.Unsigned((ulong)(to - from));
            long along = 0;
            for (int b = from; b < to; b++)
            {
                long steps = (long)Math.Round(graph.BoxAlong[b] * AlongSteps);
                w.Unsigned((ulong)(steps - along));
                along = steps;
                w.Byte((byte)graph.BoxKinds[b]);
                w.Unsigned((ulong)(StringId(graph.BoxLabels[b]) + 1));
                w.Unsigned(graph.BoxKeys[b]);
            }
        }
        w.CopyTo(output);
    }

    private static StreetGraph ReadPayload(byte[] payload)
    {
        PayloadReader r = new(payload);
        string id = r.String(), name = r.String(), attribution = r.String();
        long version = r.Signed();
        DateTimeOffset built = DateTimeOffset.FromUnixTimeMilliseconds(r.Signed());
        GeoBounds bounds = r.Bounds(), core = r.Bounds();
        GeoPoint origin = new(r.Double(), r.Double());
        StreetRegion region = new(id, name, version, built, bounds, core, origin, attribution);

        string[] table = new string[r.Count()];
        for (int i = 0; i < table.Length; i++)
            table[i] = r.String();

        int nodes = r.Count();
        double[] nodeX = new double[nodes], nodeY = new double[nodes];
        long[] nodeOsm = new long[nodes];
        long x = 0, y = 0, osm = 0;
        for (int n = 0; n < nodes; n++)
        {
            x += r.Signed();
            y += r.Signed();
            osm += r.Signed();
            nodeX[n] = x / DecimetresPerMetre;
            nodeY[n] = y / DecimetresPerMetre;
            nodeOsm[n] = osm;
        }

        int streetCount = r.Count();
        StreetInfo[] streets = new StreetInfo[streetCount];
        int[] shapeStart = new int[streetCount + 1];
        List<Flat> shape = new(streetCount * 3);
        long way = 0;
        for (int s = 0; s < streetCount; s++)
        {
            int u = r.Index(nodes), v = r.Index(nodes);
            string streetName = table[r.Index(table.Length)], fullName = table[r.Index(table.Length)];
            RoadKind kind = (RoadKind)r.Byte();
            way += r.Signed();
            int inner = r.Count();
            shapeStart[s] = shape.Count;
            Flat start = new(nodeX[u], nodeY[u]);
            shape.Add(start);
            long px = (long)Math.Round(start.X * DecimetresPerMetre), py = (long)Math.Round(start.Y * DecimetresPerMetre);
            double length = 0;
            for (int i = 0; i < inner; i++)
            {
                px += r.Signed();
                py += r.Signed();
                Flat point = new(px / DecimetresPerMetre, py / DecimetresPerMetre);
                length += Flat.Distance(shape[^1], point);
                shape.Add(point);
            }
            Flat end = new(nodeX[v], nodeY[v]);
            length += Flat.Distance(shape[^1], end);
            shape.Add(end);
            streets[s] = new StreetInfo(u, v, streetName, fullName, kind, way, length);
        }
        shapeStart[streetCount] = shape.Count;

        int[] boxStart = new int[streetCount * 2 + 1];
        List<double> along = [];
        List<LetterboxKind> kinds = [];
        List<string?> labels = [];
        List<uint> keys = [];
        for (int side = 0; side < streetCount * 2; side++)
        {
            boxStart[side] = along.Count;
            int count = r.Count();
            long steps = 0;
            for (int b = 0; b < count; b++)
            {
                steps += (long)r.Unsigned();
                along.Add(steps / (double)AlongSteps);
                kinds.Add((LetterboxKind)r.Byte());
                int label = r.Index(table.Length + 1);
                labels.Add(label == 0 ? null : table[label - 1]);
                keys.Add((uint)r.Unsigned());
            }
        }
        boxStart[streetCount * 2] = along.Count;

        return new StreetGraph(region, nodeX, nodeY, nodeOsm, streets, shapeStart, [.. shape], boxStart, [.. along], [.. kinds], [.. labels], [.. keys]);
    }

    private static (long X, long Y) Decimetres(Flat flat) =>
        ((long)Math.Round(flat.X * DecimetresPerMetre), (long)Math.Round(flat.Y * DecimetresPerMetre));

    private sealed class PayloadWriter
    {
        private readonly MemoryStream _buffer = new();

        public void Byte(byte value) => _buffer.WriteByte(value);

        public void Unsigned(ulong value)
        {
            while (value >= 0x80)
            {
                _buffer.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }
            _buffer.WriteByte((byte)value);
        }

        public void Signed(long value) => Unsigned((ulong)((value << 1) ^ (value >> 63)));

        public void Double(double value) => _buffer.Write(BitConverter.GetBytes(value));

        public void Bounds(GeoBounds bounds)
        {
            Double(bounds.South);
            Double(bounds.West);
            Double(bounds.North);
            Double(bounds.East);
        }

        public void String(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            Unsigned((ulong)bytes.Length);
            _buffer.Write(bytes);
        }

        public void CopyTo(Stream output)
        {
            _buffer.Position = 0;
            _buffer.CopyTo(output);
        }
    }

    private sealed class PayloadReader(byte[] bytes)
    {
        private int _at;

        public byte Byte() => _at < bytes.Length ? bytes[_at++] : throw Truncated();

        public ulong Unsigned()
        {
            ulong value = 0;
            for (int shift = 0; shift < 64; shift += 7)
            {
                byte next = Byte();
                value |= (ulong)(next & 0x7F) << shift;
                if (next < 0x80)
                    return value;
            }
            throw new InvalidDataException("A number in the street graph is too long.");
        }

        public long Signed()
        {
            ulong raw = Unsigned();
            return (long)(raw >> 1) ^ -(long)(raw & 1);
        }

        public int Count()
        {
            ulong value = Unsigned();
            return value <= int.MaxValue && (long)value <= bytes.Length - _at + 1L ? (int)value : throw Truncated();
        }

        public int Index(int limit)
        {
            ulong value = Unsigned();
            return value < (ulong)limit ? (int)value : throw new InvalidDataException("The street graph refers to something it doesn't contain.");
        }

        public double Double()
        {
            if (_at + 8 > bytes.Length)
                throw Truncated();
            double value = BitConverter.ToDouble(bytes, _at);
            _at += 8;
            return value;
        }

        public GeoBounds Bounds() => new(Double(), Double(), Double(), Double());

        public string String()
        {
            int length = Count();
            if (_at + length > bytes.Length)
                throw Truncated();
            string value = Encoding.UTF8.GetString(bytes, _at, length);
            _at += length;
            return value;
        }

        private static InvalidDataException Truncated() => new("The street graph file is cut short.");
    }
}
