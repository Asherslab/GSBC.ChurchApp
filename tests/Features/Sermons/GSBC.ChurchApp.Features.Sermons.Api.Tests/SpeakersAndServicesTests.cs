using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

[Integration("Which speaker a typed name lands on and which services are offered both depend on the speakers and sermons already stored")]
public sealed class SpeakersAndServicesTests : SermonsHost
{
    [Fact]
    public async Task ANewSpeakerTypedOnAnUploadLandsOnTheOneWithThatNameAndNoTwoSpeakersShareAName()
    {
        await SeedLibraryAsync();
        Guid jo = (await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Speakers.SingleAsync(speaker => speaker.Name == "Jo Ellery", Cancel))).Id;
        Guid sam = (await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Speakers.SingleAsync(speaker => speaker.Name == "Sam Rivers", Cancel))).Id;

        SpeakerRecord typed = await WithAsync(provider => provider.GetRequiredService<Speakers>().ResolveAsync("  jo   ELLERY ", Cancel));
        RpcException clash = await Assert.ThrowsAsync<RpcException>(() =>
            LibraryAsync(library => library.SaveSpeakerAsync(new SpeakerDetail { Id = sam.ToString(), Name = "jo  ellery" }).AsTask()));

        Assert.Equal(jo, typed.Id);
        Assert.Equal(StatusCode.InvalidArgument, clash.StatusCode);
    }

    [Fact]
    public async Task AServiceStaffAddIsOfferedAfterTheStandardOnesAndOtherSpellingsLandOnTheNameAlreadyUsed()
    {
        await SeedLibraryAsync();
        List<SermonRecord> sermons = await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking().OrderBy(sermon => sermon.PreachedAt).ToListAsync(Cancel));

        await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = sermons[0].Id.ToString(), Service = "Conference, Tuesday night" }).AsTask());
        SermonDetail second = await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = sermons[1].Id.ToString(), Service = " conference,  tuesday NIGHT" }).AsTask());
        SermonDetail third = await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = sermons[2].Id.ToString(), Service = "sunday night" }).AsTask());
        ServiceList offered = await LibraryAsync(library => library.ListServicesAsync(new Done()).AsTask());

        Assert.Equal(("Conference, Tuesday night", SermonServices.SundayEvening), (second.Sermon.Service, third.Sermon.Service));
        Assert.Equal([.. SermonServices.Standard, "Conference, Tuesday night"], offered.Services.Select(service => service.Name));
        Assert.Equal(2, offered.Services[^1].Count);
    }

    private Task<T> LibraryAsync<T>(Func<SermonLibraryService, Task<T>> work) => WithAsync(provider => work(provider.GetRequiredService<SermonLibraryService>()));
}
