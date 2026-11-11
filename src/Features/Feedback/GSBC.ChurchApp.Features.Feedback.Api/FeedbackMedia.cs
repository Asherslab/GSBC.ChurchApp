using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackMedia(IMediaReferences references, IMediaSystemFolders folders)
{
    private const string Jpeg = "image/jpeg";
    private const string Png = "image/png";
    private const string Webp = "image/webp";
    private const string Gif = "image/gif";
    private const string Mp4 = "video/mp4";
    private const string QuickTime = "video/quicktime";
    private const string Sqlite = "application/vnd.sqlite3";

    public static readonly MediaSystemArea Attachments = new(FeedbackAreas.Attachments, "Feedback",
        "Made by the app: screenshots and videos sent as feedback", FeedbackPermissions.Triage,
        Files:
        [
            new AreaFileType(Jpeg, FeedbackRules.LargestPicture), new AreaFileType(Png, FeedbackRules.LargestPicture),
            new AreaFileType(Webp, FeedbackRules.LargestPicture), new AreaFileType(Gif, FeedbackRules.LargestPicture),
            new AreaFileType(Mp4, FeedbackRules.LargestVideo), new AreaFileType(QuickTime, FeedbackRules.LargestVideo),
        ]);

    public static readonly MediaSystemArea PhoneData = new(FeedbackAreas.PhoneData, "Feedback phone data",
        "Made by the app: copies of a phone's stored data sent with feedback, for finding bugs", FeedbackPermissions.PhoneData,
        Files: [new AreaFileType(Sqlite, FeedbackRules.LargestPhoneData)]);

    public static string AttachmentsOwner(Guid ticket) => $"feedback.ticket/{ticket}";

    public static string PhoneDataOwner(Guid ticket) => $"feedback.phone-data/{ticket}";

    public async Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken) =>
        ids.Count == 0 ? [] : await references.VerifyAsync(ids.Select(id => new MediaRef { Id = id }).ToList(), cancellationToken);

    public async Task KeepAsync(TicketRecord ticket, IReadOnlyList<MediaRef> attachments, MediaRef? phoneData, CancellationToken cancellationToken)
    {
        if (attachments.Count > 0)
        {
            await references.UseAsync(AttachmentsOwner(ticket.Id), attachments, cancellationToken);
            await folders.FileAsync(Attachments, null, Attachments.Name, attachments, cancellationToken);
        }
        if (phoneData is not null)
        {
            await references.UseAsync(PhoneDataOwner(ticket.Id), [phoneData], cancellationToken);
            await folders.FileAsync(PhoneData, null, PhoneData.Name, [phoneData], cancellationToken);
        }
    }

    public async Task ForgetAttachmentsAsync(Guid ticket, IReadOnlyCollection<string> mediaIds, CancellationToken cancellationToken)
    {
        await references.UseAsync(AttachmentsOwner(ticket), [], cancellationToken);
        await folders.ForgetAsync(Attachments, mediaIds, cancellationToken);
    }

    public async Task ForgetPhoneDataAsync(Guid ticket, string? mediaId, CancellationToken cancellationToken)
    {
        await references.UseAsync(PhoneDataOwner(ticket), [], cancellationToken);
        if (mediaId is not null)
            await folders.ForgetAsync(PhoneData, [mediaId], cancellationToken);
    }

    public async Task ForgetUnsentAsync(DateTime uploadedBefore, CancellationToken cancellationToken)
    {
        await folders.ForgetUnusedAsync(Attachments, uploadedBefore, cancellationToken);
        await folders.ForgetUnusedAsync(PhoneData, uploadedBefore, cancellationToken);
    }
}

internal static class FeedbackPayloads
{
    public static byte[] Encode<T>(T message)
    {
        using MemoryStream stream = new();
        FeedbackContractsModel.Instance.Serialize(stream, message);
        return stream.ToArray();
    }

    public static T Decode<T>(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        return FeedbackContractsModel.Instance.Deserialize<T>(stream);
    }
}
