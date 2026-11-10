using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Chats;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed record Published(ChatRecord Chat, ChatPlace? Place, MembershipPlan Plan, MembershipFacts Facts);

internal sealed class ChatPublisher(ChatsDbContext db, ChatFacts facts, IPeopleIndex people, TimeProvider time)
{
    public async Task<Published> PlanAsync(ChatRecord chat, CancellationToken cancellationToken)
    {
        ChatPlace? place = await facts.PlaceAsync(chat.MinistryId, cancellationToken);
        MembershipFacts known = await facts.ForAsync(chat, place, cancellationToken);
        return new Published(chat, place, ChatMembership.Decide(known), known);
    }

    public async Task<Published> PublishAsync(Guid chatId, IReadOnlyCollection<Guid> changedMessages, bool cards, CancellationToken cancellationToken)
    {
        ChatRecord chat = await db.Chats.SingleAsync(found => found.Id == chatId, cancellationToken);
        Published published = await PlanAsync(chat, cancellationToken);
        MembershipPlan plan = published.Plan;
        DateTime now = time.GetUtcNow().UtcDateTime;

        List<ParticipantRecord> stored = await db.Participants.Where(participant => participant.ChatId == chatId).ToListAsync(cancellationToken);
        HashSet<Guid> receivedBefore = stored.Where(participant => participant.State == ChatStates.Active).Select(participant => participant.PersonId).ToHashSet();
        HashSet<Guid> receiveNow = plan.Recipients.Select(participant => participant.PersonId).ToHashSet();
        List<Guid> arriving = receiveNow.Except(receivedBefore).ToList();
        List<Guid> leaving = receivedBefore.Except(receiveNow).ToList();

        bool membershipChanged = Reconcile(chatId, stored, plan, now);
        bool flagsChanged = !chat.Flags.SequenceEqual(plan.Flags) || chat.HasMinors != plan.HasMinors;
        chat.Flags = plan.Flags.ToList();
        chat.HasMinors = plan.HasMinors;

        if (!membershipChanged && !flagsChanged && changedMessages.Count == 0 && !cards)
        {
            await db.SaveChangesAsync(cancellationToken);
            return published;
        }

        List<MessageRecord> messages = await db.Messages.AsNoTracking()
            .Where(message => message.ChatId == chatId && (arriving.Count > 0 || leaving.Count > 0 || changedMessages.Contains(message.Id)))
            .OrderBy(message => message.Seq)
            .ToListAsync(cancellationToken);

        foreach (Guid person in leaving)
        {
            db.FeedChanges.Add(ChatRows.CardGone(person, chatId, now));
            foreach (MessageRecord message in messages)
                db.FeedChanges.Add(ChatRows.MessageGone(person, chatId, message.Id, now));
        }

        if (receiveNow.Count > 0)
        {
            List<MessageRecord> toSend = arriving.Count > 0 ? messages : messages.Where(message => changedMessages.Contains(message.Id)).ToList();
            Dictionary<Guid, MessageRecord> replies = await RepliesAsync(toSend, cancellationToken);
            MessageRecord? last = await db.Messages.AsNoTracking().Where(message => message.ChatId == chatId)
                .OrderByDescending(message => message.Seq).FirstOrDefaultAsync(cancellationToken);
            IReadOnlyDictionary<Guid, string> names = await NamesAsync(chat, plan, toSend, replies.Values, last, cancellationToken);
            ILookup<Guid, ReactionRecord> reactions = (await ReactionsAsync(toSend, cancellationToken)).ToLookup(reaction => reaction.MessageId);

            foreach (MessageRecord message in toSend)
            {
                ChatMessage view = ChatRows.MessageView(message, names, reactions[message.Id].ToList(), message.ReplyToId is { } reply ? replies.GetValueOrDefault(reply) : null);
                byte[] payload = ChatRows.Encode(view);
                IEnumerable<Guid> to = changedMessages.Contains(message.Id) ? receiveNow : arriving;
                foreach (Guid person in to)
                    db.FeedChanges.Add(ChatRows.Message(person, view, payload, now));
            }

            Dictionary<Guid, ParticipantRecord> state = db.Participants.Local.Where(participant => participant.ChatId == chatId)
                .ToDictionary(participant => participant.PersonId);
            foreach (Participant participant in plan.Recipients)
            {
                ChatCard card = ChatCards.For(published, participant, state, names, last);
                db.FeedChanges.Add(ChatRows.Card(participant.PersonId, card, now));
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return published;
    }

    public async Task<int> PublishAllAsync(CancellationToken cancellationToken)
    {
        List<Guid> chats = await db.Chats.AsNoTracking()
            .Where(chat => chat.DeletedAt == null || db.Participants.Any(participant => participant.ChatId == chat.Id))
            .Select(chat => chat.Id)
            .ToListAsync(cancellationToken);
        foreach (Guid chat in chats)
            await PublishAsync(chat, [], false, cancellationToken);
        return chats.Count;
    }

    private bool Reconcile(Guid chatId, List<ParticipantRecord> stored, MembershipPlan plan, DateTime now)
    {
        bool changed = false;
        Dictionary<Guid, ParticipantRecord> byPerson = stored.ToDictionary(participant => participant.PersonId);
        foreach (Participant participant in plan.Participants)
        {
            if (!byPerson.Remove(participant.PersonId, out ParticipantRecord? record))
            {
                db.Participants.Add(new ParticipantRecord
                {
                    ChatId = chatId, PersonId = participant.PersonId, Role = participant.Role, State = participant.State,
                    ForChildren = participant.ForChildren.ToList(), Since = now,
                });
                changed = true;
                continue;
            }
            if (record.Role != participant.Role || record.State != participant.State || !record.ForChildren.Order().SequenceEqual(participant.ForChildren.Order()))
            {
                record.Role = participant.Role;
                record.State = participant.State;
                record.ForChildren = participant.ForChildren.ToList();
                changed = true;
            }
        }
        foreach (ParticipantRecord gone in byPerson.Values)
        {
            db.Participants.Remove(gone);
            changed = true;
        }
        return changed;
    }

    private async Task<Dictionary<Guid, MessageRecord>> RepliesAsync(List<MessageRecord> messages, CancellationToken cancellationToken)
    {
        List<Guid> ids = messages.Where(message => message.ReplyToId is not null).Select(message => message.ReplyToId!.Value).Distinct().ToList();
        return ids.Count == 0
            ? []
            : await db.Messages.AsNoTracking().Where(message => ids.Contains(message.Id)).ToDictionaryAsync(message => message.Id, cancellationToken);
    }

    private async Task<List<ReactionRecord>> ReactionsAsync(List<MessageRecord> messages, CancellationToken cancellationToken)
    {
        List<Guid> ids = messages.Select(message => message.Id).ToList();
        return ids.Count == 0 ? [] : await db.Reactions.AsNoTracking().Where(reaction => ids.Contains(reaction.MessageId)).ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> NamesAsync(
        ChatRecord chat, MembershipPlan plan, IEnumerable<MessageRecord> messages, IEnumerable<MessageRecord> replies, MessageRecord? last, CancellationToken cancellationToken)
    {
        HashSet<Guid> ids = plan.Participants.Select(participant => participant.PersonId).ToHashSet();
        foreach (MessageRecord message in messages.Concat(replies).Append(last).OfType<MessageRecord>())
        {
            if (message.SenderId is { } sender)
                ids.Add(sender);
            if (message.ForPersonId is { } child)
                ids.Add(child);
        }
        if (chat.AskerId is { } asker)
            ids.Add(asker);
        return await people.NamesAsync(ids, cancellationToken);
    }
}
