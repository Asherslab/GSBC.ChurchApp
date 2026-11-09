using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Api.Website;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using Fields = GSBC.ChurchApp.Features.Sermons.Api.SermonLibraryService.Fields;
using Reasons = GSBC.ChurchApp.Features.Sermons.Api.SermonLibraryService.Reasons;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class CorrectionTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);

    private static readonly PodcastItem Mercy = new("https://church.example/media/2026-11-01-mercy-before-judgement", "Mercy Before Judgement",
        "https://church.example/media/2026-11-01-mercy-before-judgement", new DateTime(2026, 10, 31, 14, 0, 0, DateTimeKind.Utc), "Lee Varga",
        "Sun PM - Watch at: https://www.youtube.com/watch?v=Zz9Yy8Xx7Ww", "https://files.example/2026/11/01/mercy.mp3", "audio/mpeg", 20_000_000, null);

    [Fact]
    public void StaffCorrectionsSurviveTheNextReadAndCanBeUndone()
    {
        Guid fruit = Guid.CreateVersion7();
        SermonRecord sermon = new() { Origin = SermonSources.Website, SourceSeriesId = fruit };
        WebsiteReader.Take(sermon, Mercy, null, "files.example", Now);

        SermonLibraryService.Correct(sermon, new SermonCorrection { Title = "Mercy Comes First", SeriesId = "", Service = "Conference, Tuesday night" }, false, false);
        WebsiteReader.Take(sermon, Mercy with { Title = "Mercy Before Judgment" }, null, "files.example", Now);

        Assert.Equal(("Mercy Comes First", null, "Conference, Tuesday night"), (sermon.EffectiveTitle, sermon.EffectiveSeriesId, sermon.EffectiveService));
        Assert.Equal("Mercy Before Judgment", sermon.SourceTitle);
        Assert.Equal([Fields.Title, Fields.Series, Fields.Service], SermonLibraryService.CorrectedFields(sermon));

        SermonLibraryService.Correct(sermon, new SermonCorrection { UseSource = [Fields.Title, Fields.Series] }, false, false);
        Assert.Equal(("Mercy Before Judgment", fruit, "Conference, Tuesday night"), (sermon.EffectiveTitle, sermon.EffectiveSeriesId, sermon.EffectiveService));
    }

    [Fact]
    public void ANewSermonSaysWhyItNeedsALook()
    {
        Guid newcomer = Guid.CreateVersion7(), regular = Guid.CreateVersion7();
        Dictionary<Guid, int> sermonsBy = new() { [newcomer] = 1, [regular] = 30 };

        Assert.Equal([Reasons.NewSpeaker], WhyFor(new SermonRecord { Origin = SermonSources.Website, SourceTitle = "Walking In The Light", SourceSpeakerId = newcomer }));
        Assert.Equal([Reasons.NoSeries], WhyFor(new SermonRecord { Origin = SermonSources.Website, SourceTitle = "Cross Roads - Part 7: Into Thy Hands", SourceSpeakerId = regular }));
        Assert.Equal([Reasons.JustUploaded], WhyFor(new SermonRecord { Origin = SermonSources.Uploads, SourceTitle = "The Good Soil", SourceSpeakerId = regular }));
        Assert.Equal([Reasons.New], WhyFor(new SermonRecord { Origin = SermonSources.Website, SourceTitle = "Mercy Before Judgement", SourceSpeakerId = regular }));

        List<string> WhyFor(SermonRecord sermon) => SermonLibraryService.ReasonsFor(sermon, sermonsBy);
    }
}
