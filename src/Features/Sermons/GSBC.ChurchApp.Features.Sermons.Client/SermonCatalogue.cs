using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using Grpc.Core;
using GSBC.ChurchApp.Platform.Client.Feeds;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed record SermonShelf(IReadOnlyList<Sermon> Sermons, IReadOnlyList<SeriesInfo> Series, IReadOnlyList<SpeakerInfo> Speakers)
{
    public static readonly SermonShelf Empty = new([], [], []);

    public Sermon? Find(string id) => Sermons.FirstOrDefault(sermon => sermon.Id == id);

    public SeriesInfo? FindSeries(string id) => Series.FirstOrDefault(series => series.Id == id);

    public SpeakerInfo? FindSpeaker(string id) => Speakers.FirstOrDefault(speaker => speaker.Id == id);

    public IReadOnlyList<Sermon> InSeries(string seriesId) =>
        Sermons.Where(sermon => sermon.SeriesId == seriesId).OrderBy(sermon => sermon.PreachedAt).ThenBy(sermon => sermon.Part).ToList();

    public IReadOnlyList<Sermon> BySpeaker(string speakerId) => Sermons.Where(sermon => sermon.SpeakerId == speakerId).ToList();

    public Sermon? NextInSeries(Sermon sermon) =>
        sermon.SeriesId is { } seriesId ? InSeries(seriesId).SkipWhile(other => other.Id != sermon.Id).Skip(1).FirstOrDefault() : null;

    public IReadOnlyList<Sermon> Search(string text)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
            return [];
        return Sermons.Where(sermon => words.All(word => Haystack(sermon).Contains(word, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private static string Haystack(Sermon sermon) =>
        $"{sermon.Title} {sermon.SeriesName} {sermon.SpeakerName} {sermon.Service} {sermon.PreachedAt:d MMMM yyyy}";
}

public sealed class SermonCatalogue(IServiceProvider services)
{
    private readonly ILocalFeeds? _feeds = services.GetService(typeof(ILocalFeeds)) as ILocalFeeds;

    public static readonly IReadOnlyList<string> Feeds = [SermonsFeeds.Catalogue, SermonsFeeds.Series, SermonsFeeds.Speakers];
    private const int Page = 200;

    private bool IsOnThisDevice => _feeds?.IsAvailable == true;

    public async Task<IReadOnlyList<Sermon>> LatestAsync(int count)
    {
        if (IsOnThisDevice)
            return (await LoadAsync()).Sermons.Take(count).ToList();
        if (services.GetService(typeof(ChannelBase)) is not ChannelBase channel)
            return [];
        return (await channel.CreateGrpcService<ISermonsService>().LatestAsync(new LatestQuery { Count = count })).Sermons;
    }

    public async Task<SermonShelf> LoadAsync()
    {
        if (_feeds is not { IsAvailable: true } feeds)
            return SermonShelf.Empty;
        List<Sermon> sermons = await ReadAllAsync<Sermon>(feeds, SermonsFeeds.Catalogue, descending: true);
        List<SeriesInfo> series = await ReadAllAsync<SeriesInfo>(feeds, SermonsFeeds.Series, descending: true);
        List<SpeakerInfo> speakers = await ReadAllAsync<SpeakerInfo>(feeds, SermonsFeeds.Speakers, descending: false);
        return new SermonShelf(sermons, series, speakers);
    }

    private async Task<List<T>> ReadAllAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(ILocalFeeds feeds, string feed, bool descending)
    {
        List<T> all = [];
        for (int offset = 0; ; offset += Page)
        {
            IReadOnlyList<StoredRecord> page = await feeds.ReadPageAsync(new FeedPageQuery(feed, Descending: descending, Limit: Page, Offset: offset));
            all.AddRange(page.Select(record => SermonsContractsModel.Instance.Deserialize<T>(new MemoryStream(record.Payload))));
            if (page.Count < Page)
                return all;
        }
    }
}
