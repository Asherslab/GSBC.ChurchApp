using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed class QueuedEventEdits : IDisposable
{
    public const string Service = "/gsbc.events.Editing/";
    public const string SaveEvent = Service + "SaveEvent";
    public const string SaveRegularEvent = Service + "SaveRegularEvent";
    public const string SaveDate = Service + "SaveDate";
    public const string DeleteEvent = Service + "DeleteEvent";
    public const string DeleteRegularEvent = Service + "DeleteRegularEvent";
    public const string AskToPublish = Service + "AskToPublish";
    public const string ShowOn = Service + "ShowOn";
    public const string ShowHere = Service + "ShowHere";
    public const string TellPeople = Service + "TellPeople";
    private const string Remembered = "events.queued";

    private readonly IOutbox _outbox;
    private readonly ILocalPreferences _preferences;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<QueuedEdit>? _edits;
    private IReadOnlyDictionary<string, OutboxEntry> _entries = new Dictionary<string, OutboxEntry>();

    public QueuedEventEdits(IOutbox outbox, ILocalPreferences preferences)
    {
        _outbox = outbox;
        _preferences = preferences;
        _outbox.Changed += OnOutboxChanged;
    }

    public event Action? Changed;

    public bool CanQueue => _outbox.IsAvailable;

    public async Task QueueAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string key, string method, T request)
    {
        using MemoryStream body = new();
        EventsContractsModel.Instance.Serialize(body, request);
        byte[] bytes = body.ToArray();
        await _gate.WaitAsync();
        try
        {
            List<QueuedEdit> edits = await EditsAsync();
            string entryId = await _outbox.EnqueueAsync(method, bytes);
            edits.Add(new QueuedEdit { Key = key, EntryId = entryId, Method = method, Request = bytes });
            await RememberAsync(edits);
        }
        finally
        {
            _gate.Release();
        }
        await RefreshAsync();
    }

    public async Task<IReadOnlyList<QueuedEdit>> WaitingAsync()
    {
        await RefreshAsync();
        return _edits ?? [];
    }

    public EditWaiting? WaitingFor(string key)
    {
        List<OutboxEntry> entries = (_edits ?? []).Where(edit => (Concerns(edit.Key, key) || Concerns(key, edit.Key)) && _entries.ContainsKey(edit.EntryId))
            .Select(edit => _entries[edit.EntryId]).ToList();
        if (entries.Count == 0)
            return null;
        return entries.FirstOrDefault(entry => entry.Failed) is { } failed
            ? new EditWaiting(SaveState.Failed, failed.LastError)
            : new EditWaiting(SaveState.Waiting, null);
    }

    public async Task RetryAsync(string key)
    {
        foreach (QueuedEdit edit in (_edits ?? []).Where(edit => Concerns(edit.Key, key) || Concerns(key, edit.Key)).ToList())
            if (_entries.TryGetValue(edit.EntryId, out OutboxEntry? entry) && entry.Failed)
                await _outbox.RetryAsync(entry.Id);
    }

    public static T Read<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(QueuedEdit edit) =>
        EventsContractsModel.Instance.Deserialize<T>(new MemoryStream(edit.Request));

    public static bool Concerns(string editKey, string key) => editKey == key || editKey.StartsWith(key + "/", StringComparison.Ordinal);

    public void Dispose() => _outbox.Changed -= OnOutboxChanged;

    private async Task RefreshAsync()
    {
        await _gate.WaitAsync();
        try
        {
            List<QueuedEdit> edits = await EditsAsync();
            _entries = (await _outbox.ListAsync()).ToDictionary(entry => entry.Id);
            if (edits.RemoveAll(edit => !_entries.ContainsKey(edit.EntryId)) > 0)
                await RememberAsync(edits);
        }
        finally
        {
            _gate.Release();
        }
        Changed?.Invoke();
    }

    private async Task<List<QueuedEdit>> EditsAsync()
    {
        if (_edits is not null)
            return _edits;
        string? stored = await _preferences.GetAsync(Remembered);
        _edits = stored is { Length: > 0 }
            ? EventsContractsModel.Instance.Deserialize<QueuedEdits>(new MemoryStream(Convert.FromBase64String(stored))).Edits
            : [];
        return _edits;
    }

    private async Task RememberAsync(List<QueuedEdit> edits)
    {
        using MemoryStream body = new();
        EventsContractsModel.Instance.Serialize(body, new QueuedEdits { Edits = edits });
        await _preferences.SetAsync(Remembered, edits.Count == 0 ? null : Convert.ToBase64String(body.ToArray()));
    }

    private void OnOutboxChanged() => _ = RefreshAsync();
}
