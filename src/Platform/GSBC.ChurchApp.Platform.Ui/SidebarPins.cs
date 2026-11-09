using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Platform.Ui;

public sealed class SidebarPins(IEnumerable<IPinnedPages> stores, IAccess access)
{
    private readonly IPinnedPages? _store = stores.FirstOrDefault();
    private (string Href, bool Pin)? _unsaved;

    public IReadOnlyList<string> Hrefs { get; private set; } = [];

    public IReadOnlyCollection<string> Feeds => _store?.Feeds ?? [];

    public bool Available => _store is not null && access.SignedIn;

    public bool SaveFailed => _unsaved is not null;

    public event Action? Changed;

    public bool IsPinned(string href) => Hrefs.Contains(href);

    public async Task LoadAsync()
    {
        try
        {
            Hrefs = Available ? await _store!.LoadAsync() : [];
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
        }
        Changed?.Invoke();
    }

    public async Task SetAsync(string href, bool pin)
    {
        if (!Available)
            return;
        IReadOnlyList<string> before = Hrefs;
        Hrefs = pin ? [.. Hrefs.Where(pinned => pinned != href), href] : Hrefs.Where(pinned => pinned != href).ToList();
        Changed?.Invoke();
        try
        {
            await _store!.SaveAsync(Hrefs);
            _unsaved = null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Hrefs = before;
            _unsaved = (href, pin);
        }
        Changed?.Invoke();
    }

    public Task RetryAsync() => _unsaved is var (href, pin) ? SetAsync(href, pin) : Task.CompletedTask;
}
