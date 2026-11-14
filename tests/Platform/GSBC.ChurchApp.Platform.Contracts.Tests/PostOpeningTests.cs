using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class PostOpeningTests
{
    [Fact]
    public void OnlyThePicturesAPostOpensWithLeadItAndTheRestStayInTheText()
    {
        RichText post = new()
        {
            Markdown = "![](media:a)\n![](media:b \"Set-up\")\n\n![Walk-through](https://youtu.be/aqz-KE-bpKQ)\n\nIt's been a big few months.\n\n![](media:c)",
            Media = [Picture("a"), Picture("b"), Picture("c")],
        };

        PostOpening opening = PostOpening.Of(post);

        Assert.Equal(["a", "b", null], opening.Frames.Select(frame => frame.Media?.Id));
        Assert.Equal("Set-up", opening.Frames[1].Caption);
        Assert.Equal("aqz-KE-bpKQ", opening.Frames[2].Video?.Id);
        Assert.Equal(2, opening.Blocks);
    }

    private static MediaRef Picture(string id) => new() { Id = id, ContentType = "image/jpeg", FileName = id };
}
