using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Routing.Tests;

public sealed class StreetGraphFileTests
{
    [Fact]
    public void AGraphReadsBackExactlyAsItWasWritten()
    {
        StreetGraph graph = Fixture.AlbanyCreek();

        StreetGraph again = StreetGraphFile.Read(StreetGraphFile.ToBytes(graph));

        Assert.Equal(graph.Region, again.Region);
        Assert.Equal(graph.NodeCount, again.NodeCount);
        Assert.Equal(graph.StreetCount, again.StreetCount);
        Assert.Equal(graph.LetterboxCount, again.LetterboxCount);
        for (int s = 0; s < graph.StreetCount; s++)
        {
            Assert.Equal(graph.Street(s), again.Street(s));
            Assert.Equal(graph.KeyOf(s), again.KeyOf(s));
            Assert.True(graph.Shape(s).SequenceEqual(again.Shape(s)));
        }
        for (int b = 0; b < graph.LetterboxCount; b++)
            Assert.Equal(graph.LetterboxAt(b), again.LetterboxAt(b));
    }

    [Fact]
    public void AStreetKeepsItsKeyWhenTheRegionIsRebuilt()
    {
        StreetGraph graph = Fixture.AlbanyCreek();
        int street = graph.StreetCount / 2;
        StreetKey key = graph.KeyOf(street);

        Assert.True(StreetKey.TryParse(key.ToString(), out StreetKey parsed));
        Assert.Equal(street, StreetGraphFile.Read(StreetGraphFile.ToBytes(graph, compress: false)).StreetOf(parsed));
    }

    [Fact]
    public void SomethingThatIsntAStreetGraphIsRefused()
    {
        Assert.Throws<InvalidDataException>(() => StreetGraphFile.Read("not a graph at all"u8.ToArray()));
        byte[] cut = Fixture.AlbanyCreekBytes[..(Fixture.AlbanyCreekBytes.Length / 2)];
        Assert.ThrowsAny<Exception>(() => StreetGraphFile.Read(cut));
    }

    [Fact]
    public void LetterboxCountsPerSideOnlyCountHomes()
    {
        StreetGraph graph = Fixture.AlbanyCreek();

        int homes = Enumerable.Range(0, graph.StreetCount).Sum(s => graph.Homes(s));
        int skipped = Enumerable.Range(0, graph.LetterboxCount).Count(b => graph.LetterboxKindOf(b) == LetterboxKind.Skip);

        Assert.Equal(17102, homes);
        Assert.Equal(graph.LetterboxCount, homes + skipped);
    }
}
