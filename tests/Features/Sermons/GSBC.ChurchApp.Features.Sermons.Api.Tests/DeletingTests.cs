using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Storage;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class DeletingRules
{
    private static readonly Guid Gone = Guid.CreateVersion7(), Kept = Guid.CreateVersion7(), Other = Guid.CreateVersion7();

    [Fact]
    public void ASpeakersSermonsMoveWhereStaffChoseOrToNoSpeakerAndACorrectionThatNowMatchesTheWebsiteIsCleared()
    {
        SermonRecord fromWebsite = new() { Origin = SermonSources.Website, SourceSpeakerId = Gone };
        SermonRecord corrected = new() { Origin = SermonSources.Website, SourceSpeakerId = Other, SpeakerCorrected = true, SpeakerId = Gone };
        SermonRecord correctedAway = new() { Origin = SermonSources.Website, SourceSpeakerId = Gone, SpeakerCorrected = true, SpeakerId = Kept };
        foreach (SermonRecord sermon in new[] { fromWebsite, corrected, correctedAway })
            SermonDeletion.MoveSpeaker(sermon, Gone, Kept);

        Assert.Equal((Kept, false), (fromWebsite.EffectiveSpeakerId, fromWebsite.SpeakerCorrected));
        Assert.Equal((Kept, true), (corrected.EffectiveSpeakerId, corrected.SpeakerCorrected));
        Assert.Equal((Kept, false), (correctedAway.EffectiveSpeakerId, correctedAway.SpeakerCorrected));

        SermonRecord left = new() { Origin = SermonSources.Website, SourceSpeakerId = Gone };
        SermonRecord leftCorrected = new() { Origin = SermonSources.Website, SourceSpeakerId = Other, SpeakerCorrected = true, SpeakerId = Gone };
        SermonDeletion.MoveSpeaker(left, Gone, null);
        SermonDeletion.MoveSpeaker(leftCorrected, Gone, null);

        Assert.Null(left.EffectiveSpeakerId);
        Assert.Equal((null, Other), (leftCorrected.EffectiveSpeakerId, leftCorrected.SourceSpeakerId));
    }

    [Fact]
    public void ASeriesSermonsMoveWhereStaffChoseOrOutOfAnySeries()
    {
        SermonRecord fromWebsite = new() { Origin = SermonSources.Website, SourceSeriesId = Gone };
        SermonRecord corrected = new() { Origin = SermonSources.Website, SourceSeriesId = Kept, SeriesCorrected = true, SeriesId = Gone };
        SermonDeletion.MoveSeries(fromWebsite, Gone, Kept);
        SermonDeletion.MoveSeries(corrected, Gone, Kept);

        Assert.Equal(Kept, fromWebsite.EffectiveSeriesId);
        Assert.Equal((Kept, false), (corrected.EffectiveSeriesId, corrected.SeriesCorrected));

        SermonRecord left = new() { Origin = SermonSources.Website, SourceSeriesId = Other, SeriesCorrected = true, SeriesId = Gone };
        SermonDeletion.MoveSeries(left, Gone, null);
        Assert.Equal((null, true), (left.EffectiveSeriesId, left.SeriesCorrected));
    }

    [Fact]
    public void OnlyAnUploadedFilesKeyIsTakenFromItsAddress()
    {
        Assert.Equal("sermons/audio/0123456789abcdef0123456789abcdef.mp3",
            SermonDeletion.UploadedAudioKey("https://media.example/sermons/audio/0123456789abcdef0123456789abcdef.mp3"));
        Assert.Null(SermonDeletion.UploadedAudioKey("https://files.example/2026/11/04/walking-in-the-light.mp3"));
    }
}

[Integration("Deleting a speaker or series rewrites every sermon that points at it and records the deleted id as a former id, in one transaction")]
public sealed class DeletingTests : SermonsHost
{
    [Fact]
    public async Task MovingADeletedSpeakersSermonsPutsThemOnTheOtherSpeakerAndOldLinksFollow()
    {
        await ReadTheWebsiteAsync();
        Guid jo = (await SpeakerNamedAsync("Jo Ellery"))!.Id;
        Guid lee = (await SpeakerNamedAsync("Ps Lee Varga"))!.Id;

        await LibraryAsync(library => library.DeleteSpeakerAsync(new DeleteRequest { Id = jo.ToString(), MoveTo = lee.ToString() }).AsTask());

        Assert.Null(await SpeakerNamedAsync("Jo Ellery"));
        Assert.Equal(3, (await SermonsAsync()).Count(sermon => sermon.EffectiveSpeakerId == lee));
        Assert.DoesNotContain(await LiveRowsAsync(SermonsFeeds.Speakers), row => row.RecordId == jo.ToString());
        SpeakerInfo published = SermonsPayloads.Decode<SpeakerInfo>((await LiveRowsAsync(SermonsFeeds.Speakers)).Single(row => row.RecordId == lee.ToString()).Payload);
        Assert.Contains(jo.ToString(), published.FormerIds);
    }

    [Fact]
    public async Task SermonsLeftWithoutTheirDeletedSpeakerShowNoneAndThePhotoIsLetGo()
    {
        await ReadTheWebsiteAsync();
        SpeakerRecord jo = (await SpeakerNamedAsync("Jo Ellery"))!;

        await LibraryAsync(library => library.DeleteSpeakerAsync(new DeleteRequest { Id = jo.Id.ToString() }).AsTask());

        Assert.Null(await SpeakerNamedAsync("Jo Ellery"));
        Assert.Equal(2, (await SermonsAsync()).Count(sermon => sermon.EffectiveSpeakerId is null));
        Assert.Equal([], Media.Uses[Artwork.SpeakerOwner(jo.Id)]);
    }

    [Fact]
    public async Task ASeriesMovedIntoAnotherTakesItsSermonsAndFollowersThere()
    {
        await ReadTheWebsiteAsync();
        Guid harbour = (await SeriesNamedAsync("Safe Harbour"))!.Id;
        Guid fruit = (await SeriesNamedAsync("Fruit Of The Spirit"))!.Id;
        Guid follower = Guid.CreateVersion7();
        Roles.Held.Add((follower, SermonsPermissions.SeriesFollowerRole, harbour.ToString(), true));

        SeriesDeletion asked = await LibraryAsync(library => library.SeriesDeletionAsync(new DeleteRequest { Id = harbour.ToString() }).AsTask());
        await LibraryAsync(library => library.DeleteSeriesAsync(new DeleteRequest { Id = harbour.ToString(), MoveTo = fruit.ToString() }).AsTask());

        Assert.Equal((2, 1), (asked.Sermons, asked.Followers));
        Assert.Null(await SeriesNamedAsync("Safe Harbour"));
        Assert.Equal(3, (await SermonsAsync()).Count(sermon => sermon.EffectiveSeriesId == fruit));
        Assert.Contains((follower, SermonsPermissions.SeriesFollowerRole, fruit.ToString(), true), Roles.Held);
    }

    [Fact]
    public async Task ASeriesDeletedWithItsSermonsLeftOutLetsGoOfItsFollowersAndPicture()
    {
        await ReadTheWebsiteAsync();
        Guid harbour = (await SeriesNamedAsync("Safe Harbour"))!.Id;
        Roles.Held.Add((Guid.CreateVersion7(), SermonsPermissions.SeriesFollowerRole, harbour.ToString(), true));

        await LibraryAsync(library => library.DeleteSeriesAsync(new DeleteRequest { Id = harbour.ToString() }).AsTask());

        Assert.Null(await SeriesNamedAsync("Safe Harbour"));
        Assert.All((await SermonsAsync()).Where(sermon => sermon.SourceTitle.StartsWith("Safe Harbour", StringComparison.Ordinal)), sermon => Assert.Null(sermon.EffectiveSeriesId));
        Assert.Empty(Roles.Held);
        Assert.Equal([], Media.Uses[Artwork.SeriesOwner(harbour)]);
        Assert.DoesNotContain(await LiveRowsAsync(SermonsFeeds.Series), row => row.RecordId == harbour.ToString());
    }

    [Fact]
    public async Task OnlyAHiddenUploadedSermonCanBeDeletedAndItsAudioAndEveryonesProgressGoWithIt()
    {
        await LibraryAsync(library => library.ChooseSourceAsync(new SourceChoice { Source = SermonSources.Uploads }).AsTask());
        const string Key = "sermons/audio/0123456789abcdef0123456789abcdef.mp3";
        await Blobs.PutAsync(BlobBucket.Public, Key, new MemoryStream(SampleFiles.ConstantBitrateMp3()), new BlobHeaders("audio/mpeg", "public"), Cancel);
        SermonDetail added = await LibraryAsync(library => library.AddUploadAsync(new SermonUpload
        {
            AudioKey = Key, Title = "The Good Soil", NewSpeaker = "Ruth Calder", PreachedOn = new DateTime(2026, 11, 1), Service = SermonServices.SundayMorning,
        }).AsTask());
        Guid listener = Guid.CreateVersion7();
        await WithAsync(async provider =>
        {
            SermonsDbContext db = provider.GetRequiredService<SermonsDbContext>();
            db.Progress.Add(new ProgressRecord { UserId = listener, SermonId = Guid.Parse(added.Sermon.Id), Position = 60, Duration = 1200, UpdatedAt = Clock.GetUtcNow().UtcDateTime });
            await db.SaveChangesAsync(Cancel);
        });

        RpcException shown = await Assert.ThrowsAsync<RpcException>(() => LibraryAsync(library => library.DeleteSermonAsync(new DeleteRequest { Id = added.Sermon.Id }).AsTask()));
        await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = added.Sermon.Id, Hidden = true }).AsTask());
        await LibraryAsync(library => library.DeleteSermonAsync(new DeleteRequest { Id = added.Sermon.Id }).AsTask());

        Assert.Equal(StatusCode.FailedPrecondition, shown.StatusCode);
        Assert.Empty(await SermonsAsync());
        Assert.Empty(await WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Progress.ToListAsync(Cancel)));
        Assert.Empty(await LiveRowsAsync(SermonsFeeds.Progress));
        Assert.DoesNotContain((BlobBucket.Public, Key), Blobs.Objects.Keys);
    }

    [Fact]
    public async Task ASermonFromTheWebsiteCanOnlyBeHidden()
    {
        await ReadTheWebsiteAsync();
        SermonRecord sermon = (await SermonsAsync())[0];
        await LibraryAsync(library => library.CorrectAsync(new SermonCorrection { Id = sermon.Id.ToString(), Hidden = true }).AsTask());

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => LibraryAsync(library => library.DeleteSermonAsync(new DeleteRequest { Id = sermon.Id.ToString() }).AsTask()));

        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
    }

    private Task<List<SermonRecord>> SermonsAsync() =>
        WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Sermons.AsNoTracking().ToListAsync(Cancel));

    private Task<SpeakerRecord?> SpeakerNamedAsync(string name) =>
        WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Speakers.AsNoTracking().SingleOrDefaultAsync(speaker => speaker.Name == name, Cancel));

    private Task<SeriesRecord?> SeriesNamedAsync(string name) =>
        WithAsync(provider => provider.GetRequiredService<SermonsDbContext>().Series.AsNoTracking().SingleOrDefaultAsync(series => series.Name == name, Cancel));

    private Task<T> LibraryAsync<T>(Func<SermonLibraryService, Task<T>> work) => WithAsync(provider => work(provider.GetRequiredService<SermonLibraryService>()));
}
