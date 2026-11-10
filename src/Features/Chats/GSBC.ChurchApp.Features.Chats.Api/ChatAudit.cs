using GSBC.ChurchApp.Features.Chats.Api.Data;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatAudit(ChatsDbContext db, TimeProvider time)
{
    public void Record(Guid? actor, string action, string words, Guid? chat = null, Guid? message = null, Guid? person = null) =>
        db.Audit.Add(new AuditRecord
        {
            Id = Guid.NewGuid(), At = time.GetUtcNow().UtcDateTime, ActorId = actor, Action = action, ChatId = chat, MessageId = message, PersonId = person, Words = words,
        });
}
