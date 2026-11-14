using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;
using RecordStretch = GSBC.ChurchApp.Features.Outreach.Contracts.Stretch;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class EngineInputsTests
{
    [Fact]
    public void AStretchSentToTheServerComesBackAsTheSameSideOfTheSameStreet()
    {
        StreetGraph graph = Fixture.AlbanyCreek();
        int street = Enumerable.Range(0, graph.StreetCount).First(s => graph.Homes(s, Side.Right) >= 6);
        SideStretch walked = new(street, Side.Right, 0.25, 0.75);

        RecordStretch sent = EngineInputs.Stretch(graph, "church-area", 3, walked);

        Assert.Equal(walked, EngineInputs.SideStretch(graph, sent));
        Assert.Equal(graph.KeyOf(street).ToString(), sent.Street);
        Assert.InRange(sent.Letterboxes, 1, graph.Homes(street, Side.Right));
        Assert.True(OutreachRules.IsValid(sent));
    }

    [Fact]
    public void NoJunkMailSkipsOnlyTheLetterboxAndAHostileMarkSkipsTheDoorToo()
    {
        Skip noJunkMail = new() { Address = new AddressPoint { AddressKey = 11 }, Letterbox = true };
        Skip hostile = new() { Address = new AddressPoint { AddressKey = 22 }, Letterbox = true, Door = true };
        Exclusion called = new() { Address = new AddressPoint { AddressKey = 33 }, ShowMarker = true };

        OutreachMarks marks = EngineInputs.Marks([noJunkMail, hostile], [called]);

        Assert.Equal([11u], marks.NoJunkMail);
        Assert.Equal([22u], marks.Hostile);
        Assert.Equal([33u], marks.ShownToWalkers);
    }
}
