using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed record SermonView(SermonShelf Shelf, IReadOnlyDictionary<string, ListeningEntry> Progress)
{
    public ListeningEntry? ProgressOf(Sermon sermon) => Progress.GetValueOrDefault(sermon.Id);

    public bool Heard(Sermon sermon) => ProgressOf(sermon) is { } entry && ListeningProgress.IsFinished(entry);

    public Underway? KeepGoing(DateTime nowUtc) => Client.KeepGoing.Pick(Shelf, Progress, nowUtc);
}

public sealed class SermonLibrary(SermonCatalogue catalogue, Listening listening)
{
    public static readonly IReadOnlyList<string> Feeds = [.. SermonCatalogue.Feeds, .. Listening.Feeds];

    public async Task<SermonView> LoadAsync() => new(await catalogue.LoadAsync(), await listening.LoadAsync());
}
