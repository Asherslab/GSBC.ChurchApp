using GSBC.ChurchApp.Features.Sermons.Api.Website;
using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class FeedTextTests
{
    [Theory]
    [InlineData("Sun AM - Watch at: https://youtu.be/AbCdEfGhIj0", "Jo Ellery", "Sunday morning")]
    [InlineData("SUN PM - Watch at: https://youtu.be/AbCdEfGhIj0", "Jo Ellery", "Sunday evening")]
    [InlineData("Wed PM", "Jo Ellery", "Wednesday evening")]
    [InlineData("Wed", "Jo Ellery", "Wednesday")]
    [InlineData("Sun AM - Ps Lee Varga - Watch at: https://youtu.be/AbCdEfGhIj0", "Lee Varga", "Sunday morning")]
    [InlineData("Sun 9AM - Adults Session - Watch at: https://youtu.be/AbCdEfGhIj0", "Jo Ellery", "Sunday 9am · Adults Session")]
    [InlineData("Wed 2nd Session - Youth", "Jo Ellery", "Wednesday 2nd Session · Youth")]
    [InlineData("Christmas Day - Watch at: https://youtu.be/AbCdEfGhIj0", "Jo Ellery", "Christmas Day")]
    [InlineData("Watch at: https://youtu.be/AbCdEfGhIj0", "Jo Ellery", "")]
    public void TheServiceIsReadFromTheSummary(string summary, string speaker, string service) =>
        Assert.Equal(service, ServiceLabels.FromSummary(summary, speaker));

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
    public void ResizedArtworkLeadsBackToTheOriginal()
    {
        string job = Convert.ToBase64String("""[["f","2026/11/04/abc/42140.png"],["p","podcast","1400x1400#"]]"""u8).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal("https://files.example/2026/11/04/abc/42140.png",
            TithelyArtwork.OriginalOf($"https://church.example/df_media/{job}/42140.jpg?sha=1", "files.example"));
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
