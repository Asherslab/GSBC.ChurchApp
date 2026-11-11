using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using GSBC.ChurchApp.Platform.Client.Uploads;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Feedback.Client;

public enum Sending
{
    Waiting,
    Sent,
    CouldNotSend,
}

public sealed record LastSent(string OutboxId, DateTime At, int Attachments, Sending State, string? Problem);

public sealed record FeedbackHere(int SentToday, int DailyLimit, LastSent? Last, bool Refused)
{
    public bool OverTheLimit => SentToday >= DailyLimit;
}

public sealed class FeedbackOnPhone(
    IOutbox outbox, FileUploads uploads, ILocalPreferences preferences, IAccess access, NativeBridge bridge, PhoneState state, RecentPages recent,
    TimeProvider time)
{
    private const string Method = "/gsbc.feedback.Feedback/Send";
    private const string SentKey = "feedback.sent";
    private const string PhoneDataType = "application/vnd.sqlite3";
    private static readonly TimeSpan Day = TimeSpan.FromDays(1);
    private readonly SemaphoreSlim _keeping = new(1, 1);

    public bool HoldsTheRole => access.Can(FeedbackPermissions.Send);

    public bool CanQueue => outbox.IsAvailable && uploads.CanQueue;

    public ValueTask<bool> ProvenHereAsync() => bridge.ProveInstallAsync();

    public async Task<FeedbackHere> HereAsync()
    {
        SentOnPhone sent = await SentAsync();
        DateTime since = time.GetUtcNow().UtcDateTime - Day;
        Dictionary<string, OutboxEntry> waiting = (await outbox.ListAsync()).ToDictionary(entry => entry.Id);
        LastSent? last = sent.Sent.OrderByDescending(entry => entry.At).Select(entry => Describe(entry, waiting.GetValueOrDefault(entry.OutboxId))).FirstOrDefault();
        bool refused = sent.Refused.Length > 0 || last is { State: Sending.CouldNotSend, Problem: FeedbackLimits.NotAvailable };
        return new FeedbackHere(sent.Sent.Count(entry => entry.At > since), HoldsTheRole ? FeedbackLimits.WithTheRoleADay : FeedbackLimits.PerPhoneADay, last, refused);
    }

    public async Task<string> SendAsync(string text, IReadOnlyList<MediaRef> picked)
    {
        string id = Guid.NewGuid().ToString();
        FeedbackSend send = new()
        {
            Id = id,
            Text = text.Trim(),
            Phone = new PhoneDetails
            {
                AppVersion = bridge.AppVersion ?? "", AppBuild = bridge.AppBuild ?? "", Platform = bridge.Platform ?? "web",
                OsVersion = bridge.OsVersion ?? (bridge.OsMajor > 0 ? bridge.OsMajor.ToString() : ""), Model = bridge.Model ?? "",
                DevelopmentBuild = bridge.IsDevelopmentBuild,
            },
            Page = recent.Last,
            State = await state.DescribeAsync(),
        };

        UploadTarget attachments = new(PrivateTo: FeedbackPermissions.TriageNode, PrivateToScope: id, Area: FeedbackAreas.Attachments, AreaScopeId: id);
        List<QueuedUpload> queued = picked.Select(media => uploads.For(media, MediaSlot.Id(FeedbackAreas.AttachmentIdsInSend), attachments))
            .OfType<QueuedUpload>().ToList();
        if (await uploads.KeepStoreCopyAsync(PhoneDataType) is { } copy
            && uploads.For(copy, MediaSlot.Id(FeedbackAreas.PhoneDataIdInSend),
                new UploadTarget(PrivateTo: FeedbackPermissions.PhoneDataNode, PrivateToScope: id, Area: FeedbackAreas.PhoneData, AreaScopeId: id)) is { } store)
            queued.Add(store);

        using MemoryStream body = new();
        FeedbackContractsModel.Instance.Serialize(body, send);
        string outboxId = await outbox.EnqueueAsync(Method, body.ToArray(), queued);
        await KeepAsync(sent =>
        {
            sent.Refused = "";
            sent.Sent.Add(new SentFeedback { Id = id, OutboxId = outboxId, At = time.GetUtcNow().UtcDateTime, Attachments = picked.Count });
        });
        return outboxId;
    }

    public ValueTask ForgetAsync(MediaRef media) => uploads.ForgetAsync(media);

    private static LastSent Describe(SentFeedback sent, OutboxEntry? entry) => new(
        sent.OutboxId, sent.At, sent.Attachments,
        entry is null ? Sending.Sent : entry.Failed ? Sending.CouldNotSend : Sending.Waiting,
        entry?.Failed == true ? entry.LastError : null);

    private async Task<SentOnPhone> SentAsync()
    {
        string? stored = await preferences.GetAsync(SentKey);
        if (string.IsNullOrEmpty(stored))
            return new SentOnPhone();
        using MemoryStream stream = new(Convert.FromBase64String(stored));
        SentOnPhone sent = FeedbackContractsModel.Instance.Deserialize<SentOnPhone>(stream);
        DateTime oldest = time.GetUtcNow().UtcDateTime - Day * 7;
        sent.Sent.RemoveAll(entry => entry.At < oldest);
        return sent;
    }

    private async Task KeepAsync(Action<SentOnPhone> change)
    {
        await _keeping.WaitAsync();
        try
        {
            SentOnPhone sent = await SentAsync();
            change(sent);
            using MemoryStream stream = new();
            FeedbackContractsModel.Instance.Serialize(stream, sent);
            await preferences.SetAsync(SentKey, Convert.ToBase64String(stream.ToArray()));
        }
        finally
        {
            _keeping.Release();
        }
    }
}
