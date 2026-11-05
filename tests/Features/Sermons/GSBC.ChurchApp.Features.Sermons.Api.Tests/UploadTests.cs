using System.Xml.Linq;
using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Api.Tests;

public sealed class UploadTests : SermonsHost
{
    private static readonly XNamespace Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";

    [Fact]
    public async Task UploadsAreRefusedWhileSermonsComeFromTheWebsite()
    {
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => Library(library =>
            library.BeginAudioUploadAsync(new AudioUploadRequest { FileName = "sunday.mp3", ContentType = "audio/mpeg", Bytes = 1000 }).AsTask()));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
    }

    [Fact]
    public async Task AnUploadedSermonReachesPhonesAndThePodcastFeedKeepsTheWebsitesEpisodes()
    {
        await ReadTheWebsiteAsync();
        await Library(library => library.ChooseSourceAsync(new SourceChoice { Source = SermonSources.Uploads }).AsTask());

        AudioUploadTicket ticket = await Library(library =>
            library.BeginAudioUploadAsync(new AudioUploadRequest { FileName = "sunday.mp3", ContentType = "audio/mpeg", Bytes = 16_000_000 }).AsTask());
        await Assert.ThrowsAsync<RpcException>(() => Library(library => library.AddUploadAsync(Upload(ticket.Key)).AsTask()));

        byte[] audio = [.. ConstantBitrateMp3(), .. new byte[16_000_000 - ConstantBitrateMp3().Length]];
        await Blobs.PutAsync(BlobBucket.Public, ticket.Key, new MemoryStream(audio), new BlobHeaders("audio/mpeg", ContentAddress.ImmutableCacheControl), Cancel);
        SermonDetail added = await Library(library => library.AddUploadAsync(Upload(ticket.Key)).AsTask());

        Sermon onPhones = (await CatalogueAsync()).Single(sermon => sermon.Id == added.Sermon.Id);
        Assert.Equal(("The Good Soil", "Ruth Calder", "Seeds", 1000), (onPhones.Title, onPhones.SpeakerName, onPhones.SeriesName, onPhones.DurationSeconds));

        (byte[] bytes, BlobHeaders headers) = Blobs.Objects[(BlobBucket.Public, PodcastFeedWriter.Key)];
        Assert.Equal(PodcastFeedWriter.CacheControl, headers.CacheControl);
        List<XElement> items = XDocument.Load(new MemoryStream(bytes)).Descendants("item").ToList();
        Assert.Equal(5, items.Count);
        Assert.Equal("The Good Soil", items[0].Element("title")!.Value);
        Assert.Contains(items, item => item.Element("guid")!.Value == "https://church.example/media/2026-11-01-mercy-before-judgement");
        Assert.Equal("1000", items[0].Element(Itunes + "duration")!.Value);
    }

    private Task<T> Library<T>(Func<SermonLibraryService, Task<T>> work) =>
        WithAsync(provider => work(provider.GetRequiredService<SermonLibraryService>()));

    private static SermonUpload Upload(string key) => new()
    {
        AudioKey = key,
        Title = "The Good Soil",
        NewSpeaker = "Ruth Calder",
        NewSeries = "Seeds",
        Service = "Sunday morning",
        PreachedOn = new DateTime(2026, 11, 8),
    };
}
