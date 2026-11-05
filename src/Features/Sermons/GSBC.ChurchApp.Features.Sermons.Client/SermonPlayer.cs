using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Audio;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed class SermonPlayer(IAudioPlayer player, Listening listening, SermonCatalogue catalogue)
{
    public static readonly IReadOnlyList<double> Speeds = [0.8, 1, 1.2, 1.5, 1.75, 2];

    public IAudioPlayer Audio => player;

    public bool IsLoaded(Sermon sermon) => player.State.Item?.Id == sermon.Id;

    public bool IsPlaying(Sermon sermon) => IsLoaded(sermon) && player.State.Playing;

    public async Task PlayAsync(Sermon sermon)
    {
        if (IsLoaded(sermon))
        {
            await player.PlayAsync();
            return;
        }

        IReadOnlyDictionary<string, ListeningEntry> progress = await listening.LoadAsync();
        double startAt = progress.TryGetValue(sermon.Id, out ListeningEntry? entry) && ListeningProgress.IsUnderway(entry) ? entry.Position : 0;
        await player.LoadAsync(Item(sermon), startAt, play: true);
        SermonShelf shelf = await catalogue.LoadAsync();
        await player.SetUpNextAsync(UpNext(shelf, sermon).Select(Item).ToList());
    }

    public Task ToggleAsync(Sermon sermon) =>
        IsPlaying(sermon) ? player.PauseAsync() : PlayAsync(sermon);

    public async Task MarkPlayedAsync(Sermon sermon) => await listening.MarkAsync(sermon.Id, Listening.Finished);

    public async Task MarkNotPlayedAsync(Sermon sermon)
    {
        await listening.MarkAsync(sermon.Id, Listening.Cleared);
        if (IsLoaded(sermon))
            await player.SeekAsync(0);
    }

    public Task MarkWatchedAsync(Sermon sermon) => listening.MarkAsync(sermon.Id, Listening.Watched);

    public static IReadOnlyList<Sermon> UpNext(SermonShelf shelf, Sermon sermon)
    {
        List<Sermon> next = [];
        for (Sermon? candidate = shelf.NextInSeries(sermon); candidate is not null && next.Count < 10; candidate = shelf.NextInSeries(candidate))
            next.Add(candidate);
        return next;
    }

    public static AudioItem Item(Sermon sermon) => new(
        sermon.Id,
        sermon.AudioUrl,
        sermon.Title,
        sermon.SpeakerName,
        sermon.SeriesName ?? "Good Shepherd Baptist Church",
        sermon.Artwork?.Url ?? sermon.Thumbnail?.Url,
        sermon.DurationSeconds,
        SermonsDestinations.Sermon.PathOf(sermon.Id));
}
