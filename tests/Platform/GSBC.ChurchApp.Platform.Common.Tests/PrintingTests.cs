using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GSBC.ChurchApp.Platform.Common.Printing;

namespace GSBC.ChurchApp.Platform.Common.Tests;

public sealed partial class PrintingTests
{
    public static TheoryData<int> References => [0, 1, 2];

    [Theory]
    [MemberData(nameof(References))]
    public void AQrCodeMatchesAReferenceEncoderModuleForModule(int index)
    {
        Reference reference = Load()[index];

        QrCode code = QrCode.Encode(Encoding.UTF8.GetBytes(reference.Text), reference.Mask);

        Assert.Equal(reference.Version, code.Version);
        string[] rows = Enumerable.Range(0, code.Size)
            .Select(y => new string(Enumerable.Range(0, code.Size).Select(x => code.IsDark(x, y) ? '#' : '.').ToArray())).ToArray();
        Assert.Equal(reference.Rows, rows);
    }

    [Fact]
    public void APdfsCrossReferencePointsAtEachObjectAndItsPagesCarryTheirText()
    {
        PdfDocument document = new() { Title = "Route maps" };
        document.AddPage().Text(40, 60, "Route 1 · Kingfisher St (part)", PdfFont.Bold, 14, PdfColour.Black);
        document.AddPage().Qr(QrCode.Encode("https://app.baptist.com.au/open/outreach/r/ABCDEF"), 40, 40, 80);

        byte[] bytes = document.ToBytes();
        string text = Encoding.Latin1.GetString(bytes);

        int xref = int.Parse(StartXref().Match(text).Groups[1].Value);
        string[] entries = text[xref..].Split('\n').Skip(3).TakeWhile(line => line.EndsWith("n ")).ToArray();
        Assert.NotEmpty(entries);
        for (int i = 0; i < entries.Length; i++)
            Assert.StartsWith($"{i + 1} 0 obj", text[int.Parse(entries[i][..10])..]);
        Assert.Equal(2, CountPages().Matches(text).Count);
        Assert.Contains("(Route 1 \\267 Kingfisher St \\(part\\)) Tj", Contents(bytes, text).First());
    }

    private static IEnumerable<string> Contents(byte[] bytes, string text)
    {
        foreach (Match stream in Streams().Matches(text))
        {
            int start = stream.Index + stream.Length;
            int length = int.Parse(stream.Groups[1].Value);
            using ZLibStream zlib = new(new MemoryStream(bytes, start, length), CompressionMode.Decompress);
            using StreamReader reader = new(zlib, Encoding.Latin1);
            yield return reader.ReadToEnd();
        }
    }

    private static List<Reference> Load()
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "qr-reference.json")));
        return document.RootElement.EnumerateArray().Select(item => new Reference(item.GetProperty("text").GetString()!, item.GetProperty("version").GetInt32(),
            item.GetProperty("mask").GetInt32(), item.GetProperty("rows").EnumerateArray().Select(row => row.GetString()!).ToArray())).ToList();
    }

    private sealed record Reference(string Text, int Version, int Mask, string[] Rows);

    [GeneratedRegex(@"startxref\n(\d+)")]
    private static partial Regex StartXref();

    [GeneratedRegex(@"/Type /Page /")]
    private static partial Regex CountPages();

    [GeneratedRegex(@"<< /Length (\d+) /Filter /FlateDecode >>\nstream\n")]
    private static partial Regex Streams();
}
