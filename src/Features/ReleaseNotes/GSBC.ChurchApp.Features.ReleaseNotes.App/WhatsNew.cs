using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.ReleaseNotes.App;

public sealed class WhatsNew(ILocalPreferences preferences, IReadOnlyList<ReleaseNote> notes)
{
    internal const string SeenKey = "release-notes.seen";

    public IReadOnlyList<ReleaseNote> Notes => notes;

    public bool ForStaff => BundledReleaseNotes.ForStaff;

    public IReadOnlyList<ReleaseNote> Showing { get; private set; } = [];

    public event Action? Changed;

    public async Task PresentUnseenAsync()
    {
        try
        {
            Show(await TakeUnseenHighlightsAsync());
        }
        catch (Exception)
        {
            Show([]);
        }
    }

    public void ShowLatestAgain() =>
        Show(notes.Where(note => note.Highlight).GroupBy(note => note.Version).FirstOrDefault()?.ToList() ?? []);

    public void Dismiss() => Show([]);

    internal async Task<IReadOnlyList<ReleaseNote>> TakeUnseenHighlightsAsync()
    {
        string? stored = await preferences.GetAsync(SeenKey);
        HashSet<string>? seen = stored?.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (seen is null || notes.Any(note => !seen.Contains(note.Id)))
            await preferences.SetAsync(SeenKey, string.Join('\n', (seen ?? []).Union(notes.Select(note => note.Id)).Order(StringComparer.Ordinal)));
        return seen is null ? [] : notes.Where(note => note.Highlight && !seen.Contains(note.Id)).ToList();
    }

    private void Show(IReadOnlyList<ReleaseNote> highlights)
    {
        Showing = highlights;
        Changed?.Invoke();
    }
}
