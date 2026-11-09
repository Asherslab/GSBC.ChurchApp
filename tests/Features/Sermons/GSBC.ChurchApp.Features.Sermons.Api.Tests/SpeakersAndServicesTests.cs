using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("Which speaker a website name lands on and which services are offered both depend on the sermons already stored")]
public sealed class SpeakersAndServicesTests : SermonsHost
{
    [Fact]
    public async Task AWebsiteNameLandsOnTheSpeakerItsSermonsBelongToEvenAfterARenameFollowsStaffWhenTheyMovedThemAllAndNoTwoSpeakersShareAName()
    {
        await ReadTheWebsiteAsync();
        List<SermonRecord> jos = await SermonsByAsync("Jo Ellery");
        Guid jo = jos[0].SourceSpeakerId!.Value;
        Guid sam = (await SermonsByAsync("Sam Rivers")).Single().SourceSpeakerId!.Value;
        SermonRecord lee = (await SermonsByAsync("Ps Lee Varga")).Single();

        await LibraryAsync(library => library.SaveSpeakerAsync(new SpeakerDetail { Id = jo.ToString(), Name = "Joanne Ellery" }).AsTask());
        await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = jos[0].Id.ToString(), SpeakerId = sam.ToString() }).AsTask());
        await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = lee.Id.ToString(), SpeakerId = jo.ToString() }).AsTask());
        await ReadTheWebsiteAsync(force: true);

        Assert.Equal(3, await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Speakers.CountAsync(Cancel)));
        List<SermonRecord> reread = await SermonsByAsync("Jo Ellery");
        Assert.All(reread, sermon => Assert.Equal(jo, sermon.SourceSpeakerId));
        Assert.Equal([sam, jo], reread.OrderBy(sermon => sermon.Id == jos[0].Id ? 0 : 1).Select(sermon => sermon.EffectiveSpeakerId!.Value));
        Assert.Equal(jo, (await SermonsByAsync("Ps Lee Varga")).Single().SourceSpeakerId);

        RpcException clash = await Assert.ThrowsAsync<RpcException>(() =>
            LibraryAsync(library => library.SaveSpeakerAsync(new SpeakerDetail { Id = sam.ToString(), Name = "joanne  ellery" }).AsTask()));
        Assert.Equal(StatusCode.InvalidArgument, clash.StatusCode);
    }

    [Fact]
    public async Task AServiceStaffAddIsOfferedAfterTheStandardOnesAndOtherSpellingsLandOnTheNameAlreadyUsed()
    {
        await ReadTheWebsiteAsync();
        List<SermonRecord> sermons = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking().OrderBy(sermon => sermon.PreachedAt).ToListAsync(Cancel));

        await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = sermons[0].Id.ToString(), Service = "Conference, Tuesday night" }).AsTask());
        SermonDetail second = await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = sermons[1].Id.ToString(), Service = " conference,  tuesday NIGHT" }).AsTask());
        SermonDetail third = await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = sermons[2].Id.ToString(), Service = "sunday night" }).AsTask());
        ServiceList offered = await LibraryAsync(library => library.ListServicesAsync(new Done()).AsTask());

        Assert.Equal(("Conference, Tuesday night", SermonServices.SundayEvening), (second.Sermon.Service, third.Sermon.Service));
        Assert.Equal([.. SermonServices.Standard, "Conference, Tuesday night"], offered.Services.Select(service => service.Name));
        Assert.Equal(2, offered.Services[^1].Count);
    }

    private Task<List<SermonRecord>> SermonsByAsync(string websiteName) => WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking()
        .Where(sermon => sermon.SourceSpeaker == websiteName).OrderBy(sermon => sermon.PreachedAt).ToListAsync(Cancel));

    private Task<T> LibraryAsync<T>(Func<SermonLibraryService, Task<T>> work) => WithAsync(provider => work(provider.GetRequiredService<SermonLibraryService>()));
}
