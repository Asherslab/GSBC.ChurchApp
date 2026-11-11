using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatsPersonData(ChatsDbContext db, ChatPublisher publisher, ChatAudit audit, TimeProvider time) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<ParticipantRecord> places = await db.Participants.Where(participant => participant.PersonId == userId).ToListAsync(cancellationToken);
        List<Guid> chats = places.Select(participant => participant.ChatId)
            .Union(await db.Messages.Where(message => message.SenderId == userId || message.ForPersonId == userId).Select(message => message.ChatId).Distinct().ToListAsync(cancellationToken))
            .ToList();
        foreach (ParticipantRecord place in places.Where(participant => participant.State == Contracts.ChatStates.Active))
        {
            db.FeedChanges.Add(ChatRows.CardGone(userId, place.ChatId, now));
            foreach (Guid message in await db.Messages.Where(message => message.ChatId == place.ChatId).Select(message => message.Id).ToListAsync(cancellationToken))
                db.FeedChanges.Add(ChatRows.MessageGone(userId, place.ChatId, message, now));
        }
        db.Participants.RemoveRange(places);

        foreach (MessageRecord message in await db.Messages.Where(message =>
                     message.SenderId == userId || message.ForPersonId == userId || message.DeletedBy == userId || message.Mentions.Contains(userId)).ToListAsync(cancellationToken))
        {
            if (message.SenderId == userId)
                message.SenderId = null;
            if (message.ForPersonId == userId)
                message.ForPersonId = null;
            if (message.DeletedBy == userId)
                message.DeletedBy = null;
            message.Mentions = message.Mentions.Where(person => person != userId).ToList();
        }
        await db.Versions.Where(version => version.ById == userId).ExecuteUpdateAsync(set => set.SetProperty(version => version.ById, (Guid?)null), cancellationToken);
        await db.Reactions.Where(reaction => reaction.PersonId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Consents.Where(consent => consent.ChildId == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Consents.Where(consent => consent.DecidedBy == userId).ExecuteUpdateAsync(set => set.SetProperty(consent => consent.DecidedBy, (Guid?)null), cancellationToken);
        await db.Reports.Where(report => report.ReporterId == userId).ExecuteUpdateAsync(set => set.SetProperty(report => report.ReporterId, (Guid?)null), cancellationToken);
        await db.Reports.Where(report => report.HandledBy == userId).ExecuteUpdateAsync(set => set.SetProperty(report => report.HandledBy, (Guid?)null), cancellationToken);
        await db.Chats.Where(chat => chat.CreatedBy == userId).ExecuteUpdateAsync(set => set.SetProperty(chat => chat.CreatedBy, (Guid?)null), cancellationToken);
        await db.Chats.Where(chat => chat.DeletedBy == userId).ExecuteUpdateAsync(set => set.SetProperty(chat => chat.DeletedBy, (Guid?)null), cancellationToken);
        await db.Chats.Where(chat => chat.AskerId == userId).ExecuteUpdateAsync(set => set.SetProperty(chat => chat.AskerId, (Guid?)null), cancellationToken);
        await db.Audit.Where(entry => entry.ActorId == userId).ExecuteUpdateAsync(set => set.SetProperty(entry => entry.ActorId, (Guid?)null), cancellationToken);
        await db.Audit.Where(entry => entry.PersonId == userId).ExecuteUpdateAsync(set => set.SetProperty(entry => entry.PersonId, (Guid?)null), cancellationToken);
        audit.Record(null, AuditActions.Forgotten, "An account was deleted. Its messages stay, shown as from a deleted account");
        await db.SaveChangesAsync(cancellationToken);

        List<Guid> changed = await db.Messages.Where(message => chats.Contains(message.ChatId) && message.SenderId == null).Select(message => message.Id).ToListAsync(cancellationToken);
        foreach (Guid chat in chats)
            await publisher.PublishAsync(chat, changed, true, cancellationToken);
    }
}
