using System.Text;

namespace GSBC.ChurchApp.Platform.Common.Printing;

public sealed class QrCode
{
    public const int MaxVersion = 40;
    public const int QuietZone = 4;

    private static readonly int[] EccPerBlock =
        [-1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26, 30, 22, 22, 24, 24, 28, 28, 26, 26, 26, 26, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28, 28];

    private static readonly int[] Blocks =
        [-1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5, 5, 8, 9, 9, 10, 10, 11, 13, 14, 16, 17, 17, 18, 20, 21, 23, 25, 26, 28, 29, 31, 33, 35, 37, 38, 40, 43, 45, 47, 49];

    private const int MediumFormatBits = 0;

    private readonly bool[,] _dark;
    private readonly bool[,] _function;

    private QrCode(int version)
    {
        Version = version;
        Size = version * 4 + 17;
        _dark = new bool[Size, Size];
        _function = new bool[Size, Size];
    }

    public int Version { get; }

    public int Size { get; }

    public int Mask { get; private set; }

    public bool IsDark(int x, int y) => x >= 0 && y >= 0 && x < Size && y < Size && _dark[y, x];

    public static QrCode Encode(string text) => Encode(Encoding.UTF8.GetBytes(text), null);

    internal static QrCode Encode(byte[] data, int? mask)
    {
        int version = Enumerable.Range(1, MaxVersion).FirstOrDefault(v => BitsNeeded(data.Length, v) <= DataCodewords(v) * 8);
        if (version == 0)
            throw new ArgumentException("That's too long for a QR code.", nameof(data));

        List<bool> bits = [];
        Append(bits, 0b0100, 4);
        Append(bits, data.Length, version <= 9 ? 8 : 16);
        foreach (byte b in data)
            Append(bits, b, 8);
        int capacity = DataCodewords(version) * 8;
        Append(bits, 0, Math.Min(4, capacity - bits.Count));
        Append(bits, 0, (8 - bits.Count % 8) % 8);
        for (int pad = 0xEC; bits.Count < capacity; pad ^= 0xEC ^ 0x11)
            Append(bits, pad, 8);

        byte[] codewords = new byte[bits.Count / 8];
        for (int i = 0; i < bits.Count; i++)
            if (bits[i])
                codewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));

        QrCode qr = new(version);
        qr.DrawFunctionPatterns();
        qr.DrawCodewords(Interleaved(codewords, version));
        qr.Mask = mask ?? Enumerable.Range(0, 8).MinBy(m =>
        {
            qr.ApplyMask(m);
            qr.DrawFormat(m);
            int penalty = qr.Penalty();
            qr.ApplyMask(m);
            return penalty;
        });
        qr.ApplyMask(qr.Mask);
        qr.DrawFormat(qr.Mask);
        return qr;
    }

    private static int BitsNeeded(int bytes, int version) => 4 + (version <= 9 ? 8 : 16) + bytes * 8;

    private static int RawDataModules(int version)
    {
        int result = (16 * version + 128) * version + 64;
        if (version >= 2)
        {
            int aligns = version / 7 + 2;
            result -= (25 * aligns - 10) * aligns - 55;
            if (version >= 7)
                result -= 36;
        }
        return result;
    }

    private static int DataCodewords(int version) => RawDataModules(version) / 8 - EccPerBlock[version] * Blocks[version];

    private static void Append(List<bool> bits, int value, int length)
    {
        for (int i = length - 1; i >= 0; i--)
            bits.Add(((value >> i) & 1) != 0);
    }

    private static byte[] Interleaved(byte[] data, int version)
    {
        int blocks = Blocks[version];
        int eccLength = EccPerBlock[version];
        int raw = RawDataModules(version) / 8;
        int shortBlocks = blocks - raw % blocks;
        int shortLength = raw / blocks;
        byte[] divisor = ReedSolomon.Divisor(eccLength);
        List<byte[]> made = [];
        for (int i = 0, at = 0; i < blocks; i++)
        {
            int length = shortLength - eccLength + (i < shortBlocks ? 0 : 1);
            byte[] block = data[at..(at + length)];
            at += length;
            byte[] ecc = ReedSolomon.Remainder(block, divisor);
            byte[] whole = new byte[shortLength + 1];
            block.CopyTo(whole, 0);
            ecc.CopyTo(whole, whole.Length - eccLength);
            made.Add(whole);
        }
        List<byte> result = [];
        for (int i = 0; i < made[0].Length; i++)
            for (int j = 0; j < made.Count; j++)
                if (i != shortLength - eccLength || j >= shortBlocks)
                    result.Add(made[j][i]);
        return [.. result];
    }

    private void DrawFunctionPatterns()
    {
        for (int i = 0; i < Size; i++)
        {
            Set(6, i, i % 2 == 0);
            Set(i, 6, i % 2 == 0);
        }
        Finder(3, 3);
        Finder(Size - 4, 3);
        Finder(3, Size - 4);
        int[] aligns = AlignmentCentres();
        for (int i = 0; i < aligns.Length; i++)
            for (int j = 0; j < aligns.Length; j++)
                if (!(i == 0 && j == 0) && !(i == 0 && j == aligns.Length - 1) && !(i == aligns.Length - 1 && j == 0))
                    Alignment(aligns[i], aligns[j]);
        DrawFormat(0);
        DrawVersion();
    }

    private int[] AlignmentCentres()
    {
        if (Version == 1)
            return [];
        int count = Version / 7 + 2;
        int step = Version == 32 ? 26 : (Version * 4 + count * 2 + 1) / (count * 2 - 2) * 2;
        int[] result = new int[count];
        result[0] = 6;
        for (int i = count - 1, at = Size - 7; i >= 1; i--, at -= step)
            result[i] = at;
        return result;
    }

    private void Finder(int x, int y)
    {
        for (int dy = -4; dy <= 4; dy++)
            for (int dx = -4; dx <= 4; dx++)
            {
                int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                int xx = x + dx, yy = y + dy;
                if (xx >= 0 && xx < Size && yy >= 0 && yy < Size)
                    Set(xx, yy, distance != 2 && distance != 4);
            }
    }

    private void Alignment(int x, int y)
    {
        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
                Set(x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
    }

    private void DrawFormat(int mask)
    {
        int data = (MediumFormatBits << 3) | mask;
        int rem = data;
        for (int i = 0; i < 10; i++)
            rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        int bits = ((data << 10) | rem) ^ 0x5412;
        for (int i = 0; i <= 5; i++)
            Set(8, i, Bit(bits, i));
        Set(8, 7, Bit(bits, 6));
        Set(8, 8, Bit(bits, 7));
        Set(7, 8, Bit(bits, 8));
        for (int i = 9; i < 15; i++)
            Set(14 - i, 8, Bit(bits, i));
        for (int i = 0; i < 8; i++)
            Set(Size - 1 - i, 8, Bit(bits, i));
        for (int i = 8; i < 15; i++)
            Set(8, Size - 15 + i, Bit(bits, i));
        Set(8, Size - 8, true);
    }

    private void DrawVersion()
    {
        if (Version < 7)
            return;
        int rem = Version;
        for (int i = 0; i < 12; i++)
            rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        int bits = (Version << 12) | rem;
        for (int i = 0; i < 18; i++)
        {
            int a = Size - 11 + i % 3, b = i / 3;
            Set(a, b, Bit(bits, i));
            Set(b, a, Bit(bits, i));
        }
    }

    private void DrawCodewords(byte[] codewords)
    {
        int i = 0;
        for (int right = Size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
                right = 5;
            for (int vert = 0; vert < Size; vert++)
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j;
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? Size - 1 - vert : vert;
                    if (!_function[y, x] && i < codewords.Length * 8)
                    {
                        _dark[y, x] = Bit(codewords[i >> 3], 7 - (i & 7));
                        i++;
                    }
                }
        }
    }

    private void ApplyMask(int mask)
    {
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                bool invert = mask switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => (x / 3 + y / 2) % 2 == 0,
                    5 => x * y % 2 + x * y % 3 == 0,
                    6 => (x * y % 2 + x * y % 3) % 2 == 0,
                    _ => ((x + y) % 2 + x * y % 3) % 2 == 0,
                };
                _dark[y, x] ^= invert && !_function[y, x];
            }
    }

    private int Penalty()
    {
        int result = 0;
        for (int line = 0; line < Size; line++)
        {
            result += RunPenalty(i => _dark[line, i]);
            result += RunPenalty(i => _dark[i, line]);
        }
        for (int y = 0; y < Size - 1; y++)
            for (int x = 0; x < Size - 1; x++)
            {
                bool c = _dark[y, x];
                if (c == _dark[y, x + 1] && c == _dark[y + 1, x] && c == _dark[y + 1, x + 1])
                    result += 3;
            }
        int dark = 0;
        foreach (bool module in _dark)
            if (module)
                dark++;
        int total = Size * Size;
        int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        return result + k * 10;
    }

    private int RunPenalty(Func<int, bool> at)
    {
        int result = 0;
        bool colour = false;
        int run = 0;
        int[] history = new int[7];
        for (int i = 0; i < Size; i++)
        {
            if (at(i) == colour)
            {
                run++;
                if (run == 5)
                    result += 3;
                else if (run > 5)
                    result++;
            }
            else
            {
                AddHistory(run, history);
                if (!colour)
                    result += FinderLike(history) * 40;
                colour = at(i);
                run = 1;
            }
        }
        result += FinderTerminate(colour, run, history) * 40;
        return result;
    }

    private void AddHistory(int run, int[] history)
    {
        if (history[0] == 0)
            run += Size;
        Array.Copy(history, 0, history, 1, history.Length - 1);
        history[0] = run;
    }

    private static int FinderLike(int[] h)
    {
        int n = h[1];
        bool core = n > 0 && h[2] == n && h[3] == n * 3 && h[4] == n && h[5] == n;
        return (core && h[0] >= n * 4 && h[6] >= n ? 1 : 0) + (core && h[6] >= n * 4 && h[0] >= n ? 1 : 0);
    }

    private int FinderTerminate(bool colour, int run, int[] history)
    {
        if (colour)
        {
            AddHistory(run, history);
            run = 0;
        }
        run += Size;
        AddHistory(run, history);
        return FinderLike(history);
    }

    private void Set(int x, int y, bool dark)
    {
        _dark[y, x] = dark;
        _function[y, x] = true;
    }

    private static bool Bit(int value, int i) => ((value >> i) & 1) != 0;

    private static class ReedSolomon
    {
        public static byte[] Divisor(int degree)
        {
            byte[] result = new byte[degree];
            result[degree - 1] = 1;
            int root = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < result.Length; j++)
                {
                    result[j] = Multiply(result[j], root);
                    if (j + 1 < result.Length)
                        result[j] ^= result[j + 1];
                }
                root = Multiply(root, 0x02);
            }
            return result;
        }

        public static byte[] Remainder(byte[] data, byte[] divisor)
        {
            byte[] result = new byte[divisor.Length];
            foreach (byte b in data)
            {
                int factor = b ^ result[0];
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[^1] = 0;
                for (int i = 0; i < result.Length; i++)
                    result[i] ^= Multiply(divisor[i], factor);
            }
            return result;
        }

        private static byte Multiply(int x, int y)
        {
            int z = 0;
            for (int i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }
            return (byte)z;
        }
    }
}
