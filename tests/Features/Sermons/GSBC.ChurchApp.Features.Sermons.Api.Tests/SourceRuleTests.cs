using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class SourceRuleTests : SermonsHost
{
    [Fact]
    public async Task NewSeriesAndSpeakersAreMadeHereOnlyWhileSermonsAreUploadedAndWebsiteOnesCanStillBeTidied()
    {
        await ReadTheWebsiteAsync();

        RpcException series = await Assert.ThrowsAsync<RpcException>(() => Library(library => library.SaveSeriesAsync(new SeriesDetail { Name = "Seeds" }).AsTask()));
        RpcException speaker = await Assert.ThrowsAsync<RpcException>(() => Library(library => library.SaveSpeakerAsync(new SpeakerDetail { Name = "Ruth Calder" }).AsTask()));
        Assert.Equal((StatusCode.FailedPrecondition, StatusCode.FailedPrecondition), (series.StatusCode, speaker.StatusCode));

        SeriesDetail fromWebsite = (await Library(library => library.ListSeriesAsync(new Done()).AsTask())).Series.First(one => one.FromWebsite);
        SeriesDetail renamed = await Library(library => library.SaveSeriesAsync(new SeriesDetail { Id = fromWebsite.Id, Name = "Harbours", Description = "Six weeks in the Psalms." }).AsTask());
        await ReadTheWebsiteAsync(force: true);
        SeriesDetail afterRead = (await Library(library => library.ListSeriesAsync(new Done()).AsTask())).Series.Single(one => one.Id == renamed.Id);
        Assert.Equal(("Harbours", "Six weeks in the Psalms."), (afterRead.Name, afterRead.Description));

        await Library(library => library.ChooseSourceAsync(new SourceChoice { Source = SermonSources.Uploads }).AsTask());
        Assert.Equal("Seeds", (await Library(library => library.SaveSeriesAsync(new SeriesDetail { Name = "Seeds" }).AsTask())).Name);
        Assert.Equal("Ruth Calder", (await Library(library => library.SaveSpeakerAsync(new SpeakerDetail { Name = "Ruth Calder" }).AsTask())).Name);
    }

    private Task<T> Library<T>(Func<SermonLibraryService, Task<T>> work) =>
        WithAsync(provider => work(provider.GetRequiredService<SermonLibraryService>()));
}
