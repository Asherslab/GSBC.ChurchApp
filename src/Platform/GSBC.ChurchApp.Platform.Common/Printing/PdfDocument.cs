using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace GSBC.ChurchApp.Platform.Common.Printing;

public enum PdfFont
{
    Regular,
    Bold,
}

public enum TextAlign
{
    Left,
    Centre,
    Right,
}

public readonly record struct PdfColour(double Red, double Green, double Blue)
{
    public static PdfColour Black { get; } = new(0, 0, 0);

    public static PdfColour White { get; } = new(1, 1, 1);

    public static PdfColour Grey(double level) => new(level, level, level);

    public static PdfColour Hex(string hex)
    {
        int value = int.Parse(hex.TrimStart('#'), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new PdfColour((value >> 16 & 0xFF) / 255.0, (value >> 8 & 0xFF) / 255.0, (value & 0xFF) / 255.0);
    }
}

public readonly record struct PdfPoint(double X, double Y);

public sealed class PdfDocument
{
    public const double A4Width = 595.28;
    public const double A4Height = 841.89;
    public const double PointsPerMillimetre = 72 / 25.4;

    private readonly List<PdfPage> _pages = [];

    public string Title { get; init; } = "";

    public IReadOnlyList<PdfPage> Pages => _pages;

    public PdfPage AddPage(double width = A4Width, double height = A4Height)
    {
        PdfPage page = new(width, height);
        _pages.Add(page);
        return page;
    }

    public byte[] ToBytes()
    {
        using MemoryStream output = new();
        List<long> offsets = [];
        Write(output, "%PDF-1.4\n%âãÏÓ\n");

        int fonts = 3;
        int pagesId = 2;
        int firstPage = fonts + 2;
        int objects = firstPage + _pages.Count * 2 - 1;

        void Begin(int id)
        {
            while (offsets.Count < id)
                offsets.Add(0);
            offsets[id - 1] = output.Position;
            Write(output, $"{id} 0 obj\n");
        }

        Begin(1);
        Write(output, $"<< /Type /Catalog /Pages {pagesId} 0 R >>\nendobj\n");
        Begin(pagesId);
        string kids = string.Join(" ", _pages.Select((_, i) => $"{firstPage + i * 2} 0 R"));
        Write(output, $"<< /Type /Pages /Kids [{kids}] /Count {_pages.Count} >>\nendobj\n");
        Begin(fonts);
        Write(output, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n");
        Begin(fonts + 1);
        Write(output, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n");

        for (int i = 0; i < _pages.Count; i++)
        {
            PdfPage page = _pages[i];
            int pageId = firstPage + i * 2;
            Begin(pageId);
            Write(output, string.Create(CultureInfo.InvariantCulture,
                $"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {page.Width:0.##} {page.Height:0.##}] /Resources << /Font << /F1 {fonts} 0 R /F2 {fonts + 1} 0 R >> >> /Contents {pageId + 1} 0 R >>\nendobj\n"));
            byte[] content = Deflate(page.Content);
            Begin(pageId + 1);
            Write(output, $"<< /Length {content.Length} /Filter /FlateDecode >>\nstream\n");
            output.Write(content);
            Write(output, "\nendstream\nendobj\n");
        }

        int info = objects + 1;
        Begin(info);
        Write(output, $"<< /Title {PdfPage.Literal(Title)} /Producer (GSBC Church App) >>\nendobj\n");

        long xref = output.Position;
        Write(output, $"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n");
        foreach (long offset in offsets)
            Write(output, $"{offset:D10} 00000 n \n");
        Write(output, $"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R /Info {info} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    private static byte[] Deflate(byte[] content)
    {
        using MemoryStream compressed = new();
        using (ZLibStream zlib = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(content);
        return compressed.ToArray();
    }

    private static void Write(Stream output, string text) => output.Write(Encoding.Latin1.GetBytes(text));
}

public sealed class PdfPage(double width, double height)
{
    private readonly StringBuilder _content = new();

    public double Width { get; } = width;

    public double Height { get; } = height;

    internal byte[] Content => Encoding.Latin1.GetBytes(_content.ToString());

    public void Save() => Op("q");

    public void Restore() => Op("Q");

    public void ClipTo(double x, double y, double width, double height) => Op($"{N(x)} {N(Height - y - height)} {N(width)} {N(height)} re W n");

    public void Line(IReadOnlyList<PdfPoint> points, PdfColour colour, double lineWidth, double[]? dash = null, bool round = true)
    {
        if (points.Count < 2)
            return;
        Pen(colour, lineWidth, dash, round);
        StringBuilder path = new();
        path.Append(CultureInfo.InvariantCulture, $"{N(points[0].X)} {N(Height - points[0].Y)} m");
        for (int i = 1; i < points.Count; i++)
            path.Append(CultureInfo.InvariantCulture, $" {N(points[i].X)} {N(Height - points[i].Y)} l");
        Op(path.Append(" S").ToString());
    }

    public void Rectangle(double x, double y, double width, double height, PdfColour? fill, PdfColour? stroke = null, double lineWidth = 0.5)
    {
        string box = $"{N(x)} {N(Height - y - height)} {N(width)} {N(height)} re";
        Paint(box, fill, stroke, lineWidth);
    }

    public void Polygon(IReadOnlyList<PdfPoint> points, PdfColour? fill, PdfColour? stroke = null, double lineWidth = 0.5)
    {
        if (points.Count < 3)
            return;
        StringBuilder path = new();
        path.Append(CultureInfo.InvariantCulture, $"{N(points[0].X)} {N(Height - points[0].Y)} m");
        for (int i = 1; i < points.Count; i++)
            path.Append(CultureInfo.InvariantCulture, $" {N(points[i].X)} {N(Height - points[i].Y)} l");
        Paint(path.Append(" h").ToString(), fill, stroke, lineWidth);
    }

    public void Circle(double x, double y, double radius, PdfColour? fill, PdfColour? stroke = null, double lineWidth = 0.5)
    {
        const double k = 0.5523;
        double cy = Height - y, r = radius, c = radius * k;
        string path = $"{N(x + r)} {N(cy)} m {N(x + r)} {N(cy + c)} {N(x + c)} {N(cy + r)} {N(x)} {N(cy + r)} c "
            + $"{N(x - c)} {N(cy + r)} {N(x - r)} {N(cy + c)} {N(x - r)} {N(cy)} c "
            + $"{N(x - r)} {N(cy - c)} {N(x - c)} {N(cy - r)} {N(x)} {N(cy - r)} c "
            + $"{N(x + c)} {N(cy - r)} {N(x + r)} {N(cy - c)} {N(x + r)} {N(cy)} c h";
        Paint(path, fill, stroke, lineWidth);
    }

    public void Text(double x, double y, string text, PdfFont font, double size, PdfColour colour, TextAlign align = TextAlign.Left, double angle = 0)
    {
        double width = Measure(text, font, size);
        double shift = align switch { TextAlign.Centre => width / 2, TextAlign.Right => width, _ => 0 };
        double cos = Math.Cos(angle), sin = Math.Sin(angle);
        double startX = x - shift * cos, startY = Height - y + shift * sin;
        Op($"BT {Fill(colour)} /{(font == PdfFont.Bold ? "F2" : "F1")} {N(size)} Tf {N(cos)} {N(-sin)} {N(sin)} {N(cos)} {N(startX)} {N(startY)} Tm {Literal(text)} Tj ET");
    }

    public IReadOnlyList<string> Wrap(string text, PdfFont font, double size, double width)
    {
        List<string> lines = [];
        string line = "";
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string tried = line.Length == 0 ? word : $"{line} {word}";
            if (line.Length > 0 && Measure(tried, font, size) > width)
            {
                lines.Add(line);
                line = word;
            }
            else
                line = tried;
        }
        if (line.Length > 0)
            lines.Add(line);
        return lines;
    }

    public void Qr(QrCode code, double x, double y, double size)
    {
        double module = size / (code.Size + QrCode.QuietZone * 2);
        Rectangle(x, y, size, size, PdfColour.White);
        StringBuilder path = new();
        for (int row = 0; row < code.Size; row++)
            for (int column = 0; column < code.Size; column++)
                if (code.IsDark(column, row))
                {
                    double left = x + (column + QrCode.QuietZone) * module;
                    double top = y + (row + QrCode.QuietZone) * module;
                    path.Append(CultureInfo.InvariantCulture, $"{N(left)} {N(Height - top - module)} {N(module + 0.01)} {N(module + 0.01)} re ");
                }
        Op($"{Fill(PdfColour.Black)} {path}f");
    }

    public static double Measure(string text, PdfFont font, double size)
    {
        int[] widths = font == PdfFont.Bold ? HelveticaBold : Helvetica;
        double total = 0;
        foreach (byte b in Encode(text))
            total += b is >= 32 and <= 126 ? widths[b - 32] : 556;
        return total * size / 1000;
    }

    internal static string Literal(string text)
    {
        StringBuilder result = new("(");
        foreach (byte b in Encode(text))
        {
            char c = (char)b;
            if (c is '(' or ')' or '\\')
                result.Append('\\').Append(c);
            else if (b < 32 || b > 126)
                result.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
            else
                result.Append(c);
        }
        return result.Append(')').ToString();
    }

    private static byte[] Encode(string text) => text.Select(c => c switch
    {
        '‘' => (byte)0x91,
        '’' => (byte)0x92,
        '“' => (byte)0x93,
        '”' => (byte)0x94,
        '•' => (byte)0x95,
        '–' => (byte)0x96,
        '—' => (byte)0x97,
        '×' => (byte)0xD7,
        < (char)256 => (byte)c,
        _ => (byte)'?',
    }).ToArray();

    private void Paint(string path, PdfColour? fill, PdfColour? stroke, double lineWidth)
    {
        if (fill is null && stroke is null)
            return;
        StringBuilder op = new();
        if (fill is { } f)
            op.Append(Fill(f)).Append(' ');
        if (stroke is { } s)
            op.Append(CultureInfo.InvariantCulture, $"{StrokeColour(s)} {N(lineWidth)} w [] 0 d ");
        op.Append(path).Append(fill is not null && stroke is not null ? " B" : fill is not null ? " f" : " S");
        Op(op.ToString());
    }

    private void Pen(PdfColour colour, double lineWidth, double[]? dash, bool round)
    {
        string pattern = dash is { Length: > 0 } ? $"[{string.Join(" ", dash.Select(N))}] 0 d" : "[] 0 d";
        Op($"{StrokeColour(colour)} {N(lineWidth)} w {(round ? "1 J 1 j" : "0 J 0 j")} {pattern}");
    }

    private static string Fill(PdfColour colour) => $"{N(colour.Red)} {N(colour.Green)} {N(colour.Blue)} rg";

    private static string StrokeColour(PdfColour colour) => $"{N(colour.Red)} {N(colour.Green)} {N(colour.Blue)} RG";

    private void Op(string op) => _content.Append(op).Append('\n');

    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static readonly int[] Helvetica =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556,
        278, 278, 584, 584, 584, 556, 1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667,
        611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, 333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833,
        556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584,
    ];

    private static readonly int[] HelveticaBold =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556,
        333, 333, 584, 584, 584, 611, 975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778, 667, 778, 722, 667,
        611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556, 333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889,
        611, 611, 611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584,
    ];
}
