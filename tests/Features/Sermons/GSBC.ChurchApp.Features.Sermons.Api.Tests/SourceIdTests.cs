namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class SourceIdTests
{
    [Fact]
    public void EveryDatabaseGivesTheWebsitesSermonsSeriesAndSpeakersTheSameIds()
    {
        Assert.Equal(new Guid("fb9ca4af-1603-5fac-9864-64e4704cf336"), SourceIds.Sermon(" https://church.example/media/2026-11-01-mercy-before-judgement"));
        Assert.Equal(new Guid("1ebd0f38-153e-5f8d-a12b-2fb9049e6df1"), SourceIds.Series("Safe-Harbour "));
        Assert.Equal(new Guid("8886f636-814a-5160-ad52-5fa23ad252cb"), SourceIds.Speaker(Speakers.KeyOf("  Jo   Ellery")));
    }
}
