namespace GSBC.ChurchApp.Platform.Client.Audio;

public sealed record AudioItem(string Id, string Url, string Title, string Artist, string Album, string? ArtworkUrl, double Duration, string? Link = null);

public sealed record AudioState(
    AudioItem? Item, bool Playing, bool Buffering, double Position, double Duration, double Rate,
    bool Ended, string? Problem)
{
    public static readonly AudioState Idle = new(null, false, false, 0, 0, 1, false, null);

    public double Remaining => Math.Max(0, Duration - Position);
}

public sealed record PlaybackRecord(string Id, double Position, double Duration, DateTime HeardAt, IReadOnlyDictionary<string, DateTime> Marks, long Seq);

public sealed record PlaybackJournal(IReadOnlyList<PlaybackRecord> Records, long LatestSeq);

public interface IAudioPlayer
{
    AudioState State { get; }

    IReadOnlyList<AudioItem> UpNext { get; }

    event Action? Changed;

    event Action<AudioItem>? Advanced;

    Task StartAsync();

    Task LoadAsync(AudioItem item, double startAt, bool play);

    Task PlayAsync();

    Task PauseAsync();

    Task SeekAsync(double position);

    Task SkipAsync(double seconds);

    Task SetRateAsync(double rate);

    Task SetUpNextAsync(IReadOnlyList<AudioItem> items);

    Task StopAsync();

    Task MarkAsync(string id, string mark, DateTime at);

    Task<PlaybackJournal> JournalAsync(long since);
}
