using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class SermonTextTests
{
    [Theory]
    [InlineData("Watch at: https://youtu.be/AbCdEfGhIj0", "AbCdEfGhIj0")]
    [InlineData("https://www.youtube.com/watch?v=AbCdEfGhIj0&t=30", "AbCdEfGhIj0")]
    [InlineData("https://www.youtube.com/watch?feature=share&v=AbCdEfGhIj0", "AbCdEfGhIj0")]
    [InlineData("https://www.youtube.com/live/AbCdEfGhIj0?si=x", "AbCdEfGhIj0")]
    [InlineData("https://youtube.com/embed/AbCdEfGhIj0", "AbCdEfGhIj0")]
    [InlineData("AbCdEfGhIj0", "AbCdEfGhIj0")]
    [InlineData("Wed PM", null)]
    [InlineData("https://vimeo.com/123", null)]
    public void YouTubeLinksAreFoundHoweverTheyreWritten(string text, string? id) =>
        Assert.Equal(id, YouTubeIds.Find(text));

    [Theory]
    [InlineData("Safe Harbour: Part 4 - Harbour Of Hope", "Safe Harbour", 4)]
    [InlineData("Cross Roads - Part 7: Into Thy Hands", "Cross Roads", 7)]
    [InlineData("Commandment 6: Sacred Life", null, 0)]
    public void TitlesSuggestASeriesAndAPart(string title, string? series, int part)
    {
        Assert.Equal(series, TitleParts.SuggestedSeries(title));
        Assert.Equal(part, TitleParts.PartOf(title));
    }

    [Fact]
    public void AVariableBitrateFileIsTimedFromItsXingHeader()
    {
        byte[] frame = new byte[417 * 2];
        frame[0] = 0xFF; frame[1] = 0xFB; frame[2] = 0x90; frame[3] = 0x00;
        "Xing"u8.CopyTo(frame.AsSpan(4 + 32));
        frame[4 + 32 + 7] = 1;
        BitConverter.GetBytes(System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(76_563)).CopyTo(frame, 4 + 32 + 8);
        frame[417] = 0xFF; frame[418] = 0xFB;

        Assert.Equal(2000, Mp3Duration.Seconds(frame, 999_999_999));
    }
}
