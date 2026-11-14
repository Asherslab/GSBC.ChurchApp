using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Uploads;
using GSBC.ChurchApp.Platform.Contracts.Media;
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

    [Fact]
    public async Task APictureChosenOfflineGoesWithItsSaveAndDeletingTheEventFirstDropsBoth()
    {
        Outbox outbox = new();
        FileUploads uploads = new(new Shelf(), new Services());
        StoredEventEditing editing = Editing(outbox, new EventDetail { Id = EventId, CalendarId = Calendar, Fields = new EventFields { Title = "Carols night" } }, uploads);

        MediaRef picture = (await uploads.KeepAsync(default, "carols.jpg", "image/jpeg", PickableFiles.Pictures)).Media!;
        EventSave save = EditSaves.SaveOf(EditSaves.Copy((await editing.EventAsync(Calendar, EventId))!));
        save.Fields.Picture = picture;
        await editing.SaveEventAsync(save);

        (OutboxEntry entry, IReadOnlyList<QueuedUpload> sent) = Assert.Single(outbox.Queued);
        Assert.Equal(QueuedEventEdits.SaveEvent, entry.Method);
        Assert.Equal([3, 3, 1], Assert.Single(Assert.Single(sent).Slots).Path);
        Assert.Equal(picture.Url, (await editing.EventAsync(Calendar, EventId))!.Fields.Picture?.Url);

        outbox.UploadPending();
        await editing.DeleteEventAsync(new EventRef { CalendarId = Calendar, EventId = EventId });

        Assert.Equal([entry.Id], outbox.Discarded);
        Assert.Equal(QueuedEventEdits.DeleteEvent, Assert.Single(outbox.Entries).Method);
    }

    [Fact]
    public async Task AFileAddedToTheAboutOfflineGoesWithItsSaveAndItsMediaIdLandsInUploaded()
    {
        Outbox outbox = new();
        FileUploads uploads = new(new Shelf(), new Services());
        StoredEventEditing editing = Editing(outbox, new EventDetail { Id = EventId, CalendarId = Calendar, Fields = new EventFields { Title = "Carols night" } }, uploads);

        MediaRef file = (await uploads.KeepAsync(default, "map.jpg", "image/jpeg", PickableFiles.Pictures)).Media!;
        EventSave save = EditSaves.SaveOf(EditSaves.Copy((await editing.EventAsync(Calendar, EventId))!));
        await editing.SaveEventAsync(save, [file]);

        QueuedUpload upload = Assert.Single(Assert.Single(outbox.Queued).Uploads);
        IReadOnlyList<int> slot = Assert.Single(upload.Slots).Path;
        string mediaId = Guid.CreateVersion7().ToString();
        EventSave sent = EventsContractsModel.Instance.Deserialize<EventSave>(new MemoryStream([.. Encode(save), .. Spliced(mediaId, slot)]));

        Assert.Equal([mediaId], sent.Uploaded);
        Assert.Equal("Carols night", sent.Fields.Title);
    }

    [Fact]
    public async Task ANewEventDeletedWhileItsPictureWaitsIsNeverSent()
    {
        Outbox outbox = new();
        FileUploads uploads = new(new Shelf(), new Services());
        StoredEventEditing editing = Editing(outbox, new EventDetail { Id = EventId, CalendarId = Calendar }, uploads);
        string newId = Guid.CreateVersion7().ToString();

        EventSave save = new()
        {
            CalendarId = Calendar, NewId = newId,
            Fields = new EventFields { Title = "Picnic", Picture = (await uploads.KeepAsync(default, "picnic.png", "image/png", PickableFiles.Pictures)).Media },
        };
        await editing.SaveEventAsync(save);
        outbox.UploadPending();
        await editing.DeleteEventAsync(new EventRef { CalendarId = Calendar, EventId = newId });

        Assert.Empty(outbox.Entries);
    }

    [Fact]
    public void TheMediaIdThePhoneAddsToAQueuedSaveFillsInThePictureAndKeepsTheRest()
    {
        EventSave save = new()
        {
            CalendarId = Calendar, EventId = EventId,
            Fields = new EventFields { Title = "Carols night", Picture = new MediaRef { FileName = "carols.jpg", ContentType = "image/jpeg", AltText = "Candles" } },
        };
        string mediaId = Guid.CreateVersion7().ToString();

        EventSave sent = EventsContractsModel.Instance.Deserialize<EventSave>(new MemoryStream([.. Encode(save), .. Spliced(mediaId, EventPictureSlots.EventSave.Path)]));

        Assert.Equal(mediaId, sent.Fields.Picture?.Id);
        Assert.Equal(("Carols night", "carols.jpg", "Candles", EventId), (sent.Fields.Title, sent.Fields.Picture?.FileName, sent.Fields.Picture?.AltText, sent.EventId));
    }

    private static byte[] Spliced(string value, IReadOnlyList<int> path)
    {
        byte[] inner = [.. Field(path[^1], System.Text.Encoding.UTF8.GetBytes(value))];
        for (int depth = path.Count - 2; depth >= 0; depth--)
            inner = [.. Field(path[depth], inner)];
        return inner;
    }

    private static IEnumerable<byte> Field(int number, byte[] value) => [(byte)(number << 3 | 2), (byte)value.Length, .. value];

    private static StoredEventEditing Editing(Outbox outbox, EventDetail stored, FileUploads? uploads = null)
    {
        Feeds feeds = new([new StoredRecord($"event/{stored.Id}", 1, Encode(stored), null, null, null, Calendar)]);
        return new StoredEventEditing(feeds, new QueuedEventEdits(outbox, new Preferences()), new Editor(), new CalendarDiary(new Services()),
            uploads ?? new FileUploads(new NoFileShelf(), new Services()), new Clock());
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
            ValueTask.FromResult(query is { Feed: EventsFeeds.Editing, Offset: 0 } ? editing : []);

        public ValueTask<StoreInfo> InfoAsync() => ValueTask.FromResult(new StoreInfo(0, []));

        public ValueTask RefreshAsync() => ValueTask.CompletedTask;
    }

    private sealed class Outbox : IOutbox
    {
        public List<OutboxEntry> Entries { get; } = [];
        public List<(OutboxEntry Entry, IReadOnlyList<QueuedUpload> Uploads)> Queued { get; } = [];
        public List<string> Discarded { get; } = [];
        public bool IsAvailable => true;

        public event Action? Changed;

        public ValueTask<string> EnqueueAsync(string method, byte[] request) => EnqueueAsync(method, request, []);

        public ValueTask<string> EnqueueAsync(string method, byte[] request, IReadOnlyList<QueuedUpload> uploads)
        {
            OutboxEntry entry = new(Guid.NewGuid().ToString(), method, DateTime.UnixEpoch, 0, null, false);
            Entries.Add(entry);
            Queued.Add((entry, uploads));
            return ValueTask.FromResult(entry.Id);
        }

        public void UploadPending()
        {
            HashSet<string> waiting = [.. Queued.Where(queued => queued.Uploads.Count > 0).Select(queued => queued.Entry.Id)];
            for (int index = 0; index < Entries.Count; index++)
                if (waiting.Contains(Entries[index].Id))
                    Entries[index] = Entries[index] with { WaitsForUpload = true };
            Changed?.Invoke();
        }

        public ValueTask<string> EnqueueAsync<T>(string method, T request, ProtoBuf.Meta.TypeModel model) => throw new NotSupportedException();

        public ValueTask EnqueueQuietlyAsync(string method, byte[] request) => ValueTask.CompletedTask;

        public ValueTask DiscardQuietAsync() => ValueTask.CompletedTask;

        public ValueTask<IReadOnlyList<OutboxEntry>> ListAsync() => ValueTask.FromResult<IReadOnlyList<OutboxEntry>>(Entries.ToList());

        public ValueTask RetryAsync(string id) => ValueTask.CompletedTask;

        public ValueTask DiscardAsync(string id)
        {
            Discarded.Add(id);
            Entries.RemoveAll(entry => entry.Id == id);
            return ValueTask.CompletedTask;
        }

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
        public object? GetService(Type serviceType) => serviceType == typeof(IUploadRequests) ? new Requests() : null;
    }

    private sealed class Shelf : IFileShelf
    {
        public bool IsAvailable => true;

        public ValueTask<StagedFile?> KeepAsync(Microsoft.AspNetCore.Components.ElementReference input, string contentType, int index = 0)
        {
            string token = Guid.NewGuid().ToString();
            return ValueTask.FromResult<StagedFile?>(new StagedFile(token, "picture", contentType, 1200, "ab12", $"capacitor://localhost/_capacitor_file_/Uploads/{token}"));
        }

        public ValueTask<StagedFile?> CopyStoreAsync(string contentType) => ValueTask.FromResult<StagedFile?>(null);

        public ValueTask ForgetAsync(string token) => ValueTask.CompletedTask;
    }

    private sealed class Requests : IUploadRequests
    {
        public string Accepted(PickableFiles kinds) => "image/jpeg,image/png";

        public string? ContentTypeOf(string fileName, string reported, PickableFiles kinds) => reported;

        public string Unusable(string fileName, PickableFiles kinds) => "No.";

        public QueuedUpload For(StagedFile file, UploadTarget target, IReadOnlyList<MediaSlot> slots) =>
            new(file, new QueuedCall("find", []), new QueuedCall("begin", []), new QueuedCall("complete", []), slots);
    }
}
