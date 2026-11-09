using GSBC.ChurchApp.Features.Sermons.Api.Data;
using Reasons = GSBC.ChurchApp.Features.Sermons.Api.SermonLibraryService.Reasons;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class CorrectionTests
{
    [Fact]
    public void ANewSermonSaysWhyItNeedsALook()
    {
        Guid newcomer = Guid.CreateVersion7(), regular = Guid.CreateVersion7();
        Dictionary<Guid, int> sermonsBy = new() { [newcomer] = 1, [regular] = 30 };

        Assert.Equal([Reasons.JustUploaded, Reasons.NewSpeaker], WhyFor(new SermonRecord { Title = "Walking In The Light", SpeakerId = newcomer }));
        Assert.Equal([Reasons.JustUploaded, Reasons.NoSeries], WhyFor(new SermonRecord { Title = "Cross Roads - Part 7: Into Thy Hands", SpeakerId = regular }));
        Assert.Equal([Reasons.JustUploaded], WhyFor(new SermonRecord { Title = "The Good Soil", SpeakerId = regular }));

        List<string> WhyFor(SermonRecord sermon) => SermonLibraryService.ReasonsFor(sermon, sermonsBy);
    }
}
