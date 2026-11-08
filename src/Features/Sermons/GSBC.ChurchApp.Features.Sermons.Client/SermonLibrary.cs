using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Audio;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed record SermonView(SermonShelf Shelf, IReadOnlyDictionary<string, ListeningEntry> Progress, IReadOnlyDictionary<string, AudioDownload> Downloads)
{
    public AudioDownload? DownloadOf(Sermon sermon) => IdsOf(sermon).Select(Downloads.GetValueOrDefault).FirstOrDefault(download => download is not null);

    public bool Downloaded(Sermon sermon) => DownloadOf(sermon)?.State == DownloadState.Done;

    public ListeningEntry? ProgressOf(Sermon sermon) => IdsOf(sermon).Select(Progress.GetValueOrDefault).Aggregate((ListeningEntry?)null, ListeningProgress.MergeEither);

    public bool Heard(Sermon sermon) => ProgressOf(sermon) is { } entry && ListeningProgress.IsFinished(entry);

    private static IEnumerable<string> IdsOf(Sermon sermon) => [sermon.Id, .. sermon.FormerIds];

    public Underway? KeepGoing(DateTime nowUtc) => Client.KeepGoing.Pick(Shelf, Progress, nowUtc);
}

public sealed class SermonLibrary(SermonCatalogue catalogue, Listening listening, IAudioDownloads downloads)
{
    public static readonly IReadOnlyList<string> Feeds = [.. SermonCatalogue.Feeds, .. Listening.Feeds];

    public const string Remembered = "sermons.library";

    public IAudioDownloads Downloads => downloads;

    public async Task<SermonView> LoadAsync() => new(await catalogue.LoadAsync(), await listening.LoadAsync(), await downloads.ListAsync());
}
