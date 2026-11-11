using System.Security.Claims;
using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatMedia(IMediaReferences references, IMediaSystemFolders folders)
{
    public static readonly MediaSystemArea Area = new(ChatFiles.Area, "Chat files", "Made by the app: photos and files people share in chats",
        ChatsPermissions.Member);

    public async Task<List<StoredAttachment>> VerifyAsync(IReadOnlyCollection<Attachment> claimed, CancellationToken cancellationToken)
    {
        List<MediaRef> wanted = claimed.Where(attachment => attachment.MediaId.Length > 0).Select(attachment => new MediaRef { Id = attachment.MediaId }).ToList();
        if (wanted.Count != claimed.Count)
            throw ChatErrors.Invalid("A photo or file in that message wasn't uploaded. Try sending it again.");
        return wanted.Count == 0
            ? []
            : (await references.VerifyAsync(wanted, cancellationToken)).Select(media => new StoredAttachment
            {
                MediaId = media.Id, Key = media.Key, Name = media.FileName, ContentType = media.ContentType, Bytes = media.Bytes,
                Width = media.Width, Height = media.Height,
            }).ToList();
    }

    public async Task KeepAsync(MessageRecord message, string chatName, CancellationToken cancellationToken)
    {
        List<MediaRef> used = message.Attachments.Select(attachment => new MediaRef { Id = attachment.MediaId }).ToList();
        if (used.Count == 0)
            return;
        await references.UseAsync($"chats.message/{message.Id}", used, cancellationToken);
        await folders.FileAsync(Area, message.ChatId.ToString(), chatName, used, cancellationToken);
    }
}

internal sealed class ChatMediaAccess(ChatsDbContext db, ChatPublisher publisher, IAccessEvaluator access) : IMediaAreaAccess
{
    public string Area => ChatFiles.Area;

    public async Task<bool> MayUploadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken) =>
        await PlanOfAsync(user, scopeId, cancellationToken) is ({ } plan, { } chat, Guid me)
        && (ChatMembership.MaySend(plan, chat.Kind, me) || plan.Of(me) is { ForChildren.Count: > 0 } reader
            && reader.ForChildren.Any(child => ChatMembership.MaySendFor(plan, chat.Kind, me, child)));

    public async Task<bool> MayReadAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken)
    {
        if (await PlanOfAsync(user, scopeId, cancellationToken) is not ({ } plan, { } chat, Guid me))
            return false;
        if (plan.Of(me) is { Receives: true })
            return true;
        GrantSet grants = await access.GrantsForAsync(me, cancellationToken);
        return ChatOversight.MayCheck(grants, chat);
    }

    private async Task<(MembershipPlan?, ChatRecord?, Guid)> PlanOfAsync(ClaimsPrincipal user, string scopeId, CancellationToken cancellationToken)
    {
        if (Caller.UserId(user) is not { } me || !Guid.TryParse(scopeId, out Guid chatId)
            || await db.Chats.AsNoTracking().SingleOrDefaultAsync(chat => chat.Id == chatId, cancellationToken) is not { } chat)
            return (null, null, Guid.Empty);
        return ((await publisher.PlanAsync(chat, cancellationToken)).Plan, chat, me);
    }
}
