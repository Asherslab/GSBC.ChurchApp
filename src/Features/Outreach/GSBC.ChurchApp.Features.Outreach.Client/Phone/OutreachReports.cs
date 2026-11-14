using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Uploads;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed class OutreachReports(IOutbox outbox, FileUploads uploads, OutreachStore store)
{
    private const string Method = "/gsbc.outreach.Outreach/Report";
    private const int PhotoField = 5;

    public bool TakesPhotos => uploads.CanQueue;

    public async Task SendAsync(ReportReason reason, AddressPoint place, string note, MediaRef? photo)
    {
        ReportSend send = new() { Id = Guid.NewGuid().ToString(), Reason = reason, Place = place, Note = OutreachRules.Clip(note, OutreachRules.LongestNote) };
        string? ministry = (await store.LoadAsync()).Settings.MinistryIds.FirstOrDefault();
        UploadTarget target = new("A place reported on an outreach walk", OutreachPermissions.ReviewNode, ministry, OutreachAreas.ReportPhotos, ministry);
        List<QueuedUpload> queued = [.. new[] { uploads.For(photo, MediaSlot.Ref(PhotoField), target) }.OfType<QueuedUpload>()];
        using MemoryStream body = new();
        OutreachContractsModel.Instance.Serialize(body, send);
        await outbox.EnqueueAsync(Method, body.ToArray(), queued);
    }

    public ValueTask ForgetAsync(MediaRef? photo) => uploads.ForgetAsync(photo);
}
