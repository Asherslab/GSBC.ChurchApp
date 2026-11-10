using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatScopeObjects(ChatsDbContext db) : IScopeObjects
{
    public ScopeKind Kind => ScopeKind.Chat;

    public async ValueTask<bool> ExistsAsync(string objectId, CancellationToken cancellationToken) =>
        Guid.TryParse(objectId, out Guid id) && await db.Chats.AnyAsync(chat => chat.Id == id && chat.DeletedAt == null, cancellationToken);
}
