using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class OversightMarks(ChatsDbContext db, TimeProvider time)
{
    public void Touch(ChatRecord chat)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        Add($"leaders:{chat.Id:N}", Audience.HoldingAt(ChatsPermissions.CheckNode, Scope.Of(ScopeKind.Ministry, chat.MinistryId.ToString())), now);
        Add($"auditors:{chat.Id:N}", Audience.Holding(ChatsPermissions.AuditNode), now);
    }

    private void Add(string id, Audience audience, DateTime now)
    {
        FeedChangeRecord marker = FeedChangeRecord.Marker(ChatsFeeds.Oversight, id, now);
        marker.ScopeId = audience.Text;
        db.FeedChanges.Add(marker);
    }
}
