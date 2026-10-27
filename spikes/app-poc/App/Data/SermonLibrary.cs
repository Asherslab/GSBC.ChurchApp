using System.Diagnostics;

namespace Gsbc.AppPoc.Data;

public enum FeedSource { None, Bundled, Live }

public sealed class SermonLibrary(HttpClient http, SampleContent content)
{
    public const string LiveFeedUrl = "https://baptist.com.au/podcasts/media.rss";
    private const string BundledFeedPath = "data/media.rss";
    private static readonly TimeSpan LiveTimeout = TimeSpan.FromSeconds(10);

    private Task? loading;

    public IReadOnlyList<Sermon> Sermons { get; private set; } = [];
    public FeedSource Source { get; private set; }
    public string? LastError { get; private set; }
    public TimeSpan? LastFetch { get; private set; }

    public event Action? Changed;

    public Task EnsureLoadedAsync() => loading ??= LoadAsync();

    public Sermon? Find(string id) => Sermons.FirstOrDefault(s => s.Id == id);

    public SermonNotes? NotesFor(Sermon sermon) => content.SermonNotes.GetValueOrDefault(sermon.Id);

    public string? PhotoFor(Sermon sermon) => content.SpeakerPhotos.GetValueOrDefault(sermon.Speaker);

    public IEnumerable<IGrouping<string, Sermon>> BySeries() =>
        Sermons.Where(s => s.Series is not null).GroupBy(s => s.Series!);

    public async Task RefreshAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var cancel = new CancellationTokenSource(LiveTimeout);
            var xml = await http.GetStringAsync(LiveFeedUrl, cancel.Token);
            Apply(SermonFeed.Parse(xml), FeedSource.Live);
            LastError = null;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or System.Xml.XmlException)
        {
            LastError = e.Message;
            Changed?.Invoke();
        }
        LastFetch = stopwatch.Elapsed;
    }

    private async Task LoadAsync()
    {
        Apply(SermonFeed.Parse(await http.GetStringAsync(BundledFeedPath)), FeedSource.Bundled);
        _ = RefreshAsync();
    }

    private void Apply(IReadOnlyList<Sermon> sermons, FeedSource source)
    {
        if (sermons.Count == 0)
        {
            return;
        }
        Sermons = sermons;
        Source = source;
        Changed?.Invoke();
    }
}
