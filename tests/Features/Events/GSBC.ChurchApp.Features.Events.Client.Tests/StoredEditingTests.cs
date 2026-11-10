using GSBC.ChurchApp.Features.Events.Client;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Events.Client.Tests;

public sealed class StoredEditingTests
{
    private static readonly string Calendar = Guid.CreateVersion7().ToString();
    private static readonly string EventId = Guid.CreateVersion7().ToString();

    [Fact]
    public async Task WithNoConnectionAnEditorOpensFromThePhonesCopyAndASaveWaitsInTheOutboxThenShowsWhenReopened()
    {
        Outbox outbox = new();
        StoredEventEditing editing = Editing(outbox, new EventDetail
        {
            Id = EventId, CalendarId = Calendar, Fields = new EventFields { Title = "Carols night" },
            Times = new EventTimes { StartDate = "2026-12-20", StartMinute = 18 * 60, EndMinute = 20 * 60 }, Showing = Showing.Shown,
        });

        EventDetail opened = (await editing.EventAsync(Calendar, EventId))!;
        EventSave save = EditSaves.SaveOf(EditSaves.Copy(opened));
        save.Fields.Title = "Carols by candlelight";
        save.Base = EditSaves.Copy(EditSaves.SaveOf(opened));
        EditSent sent = await editing.SaveEventAsync(save);
        EventDetail reopened = (await editing.EventAsync(Calendar, EventId))!;

        Assert.True(sent is { Succeeded: true, Queued: true });
        Assert.Equal(QueuedEventEdits.SaveEvent, Assert.Single(outbox.Entries).Method);
        Assert.Equal("Carols by candlelight", reopened.Fields.Title);
        Assert.Equal(SaveState.Waiting, editing.WaitingFor(EditKeys.Event(EventId))?.State);

        outbox.Fail("That event isn't here any more.");
        await editing.RegularEventsAsync(Calendar);
        Assert.Equal(new EditWaiting(SaveState.Failed, "That event isn't here any more."), editing.WaitingFor(EditKeys.Event(EventId)));

        outbox.Sent();
        await editing.RegularEventsAsync(Calendar);
        Assert.Null(editing.WaitingFor(EditKeys.Event(EventId)));
    }

    private static StoredEventEditing Editing(Outbox outbox, EventDetail stored)
    {
        Feeds feeds = new([new StoredRecord($"event/{stored.Id}", 1, Encode(stored), null, null, null, Calendar)]);
        return new StoredEventEditing(feeds, new QueuedEventEdits(outbox, new Preferences()), new Editor(), new CalendarDiary(new Services()), new Clock());
    }

    private static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        EventsContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    private sealed class Feeds(IReadOnlyList<StoredRecord> editing) : ILocalFeeds
    {
        public SyncState State => SyncState.Offline;
        public bool IsAvailable => true;

        public event Action<IReadOnlySet<string>>? FeedsChanged { add { } remove { } }
        public event Action? StateChanged { add { } remove { } }

        public void Follow(IEnumerable<string> feeds) { }

        public ValueTask<IReadOnlyList<StoredRecord>> ReadPageAsync(FeedPageQuery query) =>
            ValueTask.FromResult(query.Feed == EventsFeeds.Editing && query.Offset == 0 ? editing : []);

        public ValueTask<StoreInfo> InfoAsync() => ValueTask.FromResult(new StoreInfo(0, []));

        public ValueTask RefreshAsync() => ValueTask.CompletedTask;
    }

    private sealed class Outbox : IOutbox
    {
        public List<OutboxEntry> Entries { get; } = [];
        public bool IsAvailable => true;

        public event Action? Changed;

        public ValueTask<string> EnqueueAsync(string method, byte[] request)
        {
            string id = Guid.NewGuid().ToString();
            Entries.Add(new OutboxEntry(id, method, DateTime.UnixEpoch, 0, null, false));
            return ValueTask.FromResult(id);
        }

        public ValueTask<string> EnqueueAsync<T>(string method, T request, ProtoBuf.Meta.TypeModel model) => throw new NotSupportedException();

        public ValueTask EnqueueQuietlyAsync(string method, byte[] request) => ValueTask.CompletedTask;

        public ValueTask DiscardQuietAsync() => ValueTask.CompletedTask;

        public ValueTask<IReadOnlyList<OutboxEntry>> ListAsync() => ValueTask.FromResult<IReadOnlyList<OutboxEntry>>(Entries.ToList());

        public ValueTask RetryAsync(string id) => ValueTask.CompletedTask;

        public ValueTask DiscardAsync(string id) => ValueTask.CompletedTask;

        public void Fail(string error)
        {
            Entries[0] = Entries[0] with { Failed = true, LastError = error, Attempts = 1 };
            Changed?.Invoke();
        }

        public void Sent()
        {
            Entries.Clear();
            Changed?.Invoke();
        }
    }

    private sealed class Preferences : ILocalPreferences
    {
        private readonly Dictionary<string, string?> _values = [];

        public ValueTask<string?> GetAsync(string key) => ValueTask.FromResult(_values.GetValueOrDefault(key));

        public ValueTask SetAsync(string key, string? value)
        {
            _values[key] = value;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class Editor : IAccess
    {
        public event Action? Changed { add { } remove { } }
        public bool SignedIn => true;

        public bool Can(Permission permission, string? objectId = null) => permission.Node == EventsPermissions.EditNode;

        public bool CanSomewhere(Permission permission) => Can(permission);
    }

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class Services : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
