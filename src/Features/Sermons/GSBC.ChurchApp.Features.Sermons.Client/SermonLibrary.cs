using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Audio;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed record SermonView(SermonShelf Shelf, IReadOnlyDictionary<string, ListeningEntry> Progress, IReadOnlyDictionary<string, AudioDownload> Downloads)
{
    public AudioDownload? DownloadOf(Sermon sermon) => Downloads.GetValueOrDefault(sermon.Id);

    public bool Downloaded(Sermon sermon) => DownloadOf(sermon)?.State == DownloadState.Done;

    public ListeningEntry? ProgressOf(Sermon sermon) => Progress.GetValueOrDefault(sermon.Id);

    public bool Heard(Sermon sermon) => ProgressOf(sermon) is { } entry && ListeningProgress.IsFinished(entry);

    public Underway? KeepGoing(DateTime nowUtc) => Client.KeepGoing.Pick(Shelf, Progress, nowUtc);
}

public sealed class SermonLibrary(SermonCatalogue catalogue, Listening listening, IAudioDownloads downloads)
{
    public static readonly IReadOnlyList<string> Feeds = [.. SermonCatalogue.Feeds, .. Listening.Feeds];

    public IAudioDownloads Downloads => downloads;

    public async Task<SermonView> LoadAsync() => new(await catalogue.LoadAsync(), await listening.LoadAsync(), await downloads.ListAsync());
}
