using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Chats;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Chats;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatActions(
    ChatsDbContext db, ChatFacts facts, ChatPublisher publisher, ChatNotifier notifier, ChatAudit audit, OversightMarks marks, ChatMedia media,
    IRoleAssignments roles, IFamilies families, IPeopleIndex people, TimeProvider time)
{
    public async Task StartAsync(Guid me, StartChat request, CancellationToken cancellationToken)
    {
        Guid chatId = ChatErrors.IdOf(request.ChatId, "chat");
        if (await db.Chats.AnyAsync(chat => chat.Id == chatId, cancellationToken))
            return;

        Guid ministryId = ChatErrors.IdOf(request.MinistryId, "ministry");
        ChatPlace place = await facts.PlaceAsync(ministryId, cancellationToken) ?? throw ChatErrors.Gone("That ministry was deleted.");
        if (!place.ChatsOn)
            throw ChatErrors.Refused(ChatErrors.ChatsOff);
        bool leads = await facts.IsAllowedAsync(me, ChatsPermissions.Manage, ministryId.ToString(), cancellationToken);
        if (!leads && !(place.TeamMayStart && await facts.InAudienceAsync(me, place, ChatAudiences.Team, cancellationToken)))
            throw ChatErrors.Refused(place.TeamMayStart ? "Only the ministry's team can start a chat." : "Only the ministry's leaders can start a chat.");
        if (!leads && (request.Kind != ChatKinds.Group || request.Audience != ChatAudiences.Team))
            throw ChatErrors.Refused("On the team you can start a group chat for the team. Ask a leader for a channel or a chat with attendees or parents.");
        string name = NameOf(request.Name);
        if (request.Kind is not (ChatKinds.Group or ChatKinds.Channel))
            throw ChatErrors.Invalid("Choose a group chat or a channel.");
        if (!ChatAudienceRules.IsValid(request.Audience, place))
            throw ChatErrors.Invalid("Choose who the chat is for.");

        ChatRecord chat = new()
        {
            Id = chatId, MinistryId = ministryId, Name = name, Kind = request.Kind, Audience = request.Audience, CreatedBy = me,
            CreatedAt = time.GetUtcNow().UtcDateTime,
        };
        db.Chats.Add(chat);
        audit.Record(me, AuditActions.Started, $"Started \"{name}\" for {AudienceWords(request.Audience)}", chatId);
        marks.Touch(chat);
        await db.SaveChangesAsync(cancellationToken);

        if (chat.Audience == ChatAudiences.Chosen)
        {
            foreach (Guid person in request.People.Select(text => ChatErrors.IdOf(text, "person")).Append(me).Distinct())
                await roles.GiveAsFeatureAsync(person, ChatsPermissions.MemberRole, chatId.ToString(), null, cancellationToken);
        }

        if (request.FirstMessage.Trim().Length > 0)
            await SendAsync(me, new SendMessage { ChatId = chat.Id.ToString(), ClientId = $"start:{chatId:N}", Text = request.FirstMessage }, cancellationToken);
        else
            await publisher.PublishAsync(chatId, [], true, cancellationToken);
    }

    public async Task ChangeAsync(Guid me, ChangeChat request, CancellationToken cancellationToken)
    {
        ChatRecord chat = await ManagedAsync(me, request.ChatId, cancellationToken);
        string name = NameOf(request.Name);
        string audience = request.Audience.Length == 0 ? chat.Audience : request.Audience;
        if (chat.Kind == ChatKinds.Ask && audience != chat.Audience)
            throw ChatErrors.Invalid("A conversation about joining stays with the person who asked and the leaders.");
        ChatPlace place = await facts.PlaceAsync(chat.MinistryId, cancellationToken) ?? throw ChatErrors.Gone("That ministry was deleted.");
        if (!ChatAudienceRules.IsValid(audience, place))
            throw ChatErrors.Invalid("Choose who the chat is for.");
        if (audience != chat.Audience && audience == ChatAudiences.Chosen)
            await roles.GiveAsFeatureAsync(me, ChatsPermissions.MemberRole, chat.Id.ToString(), null, cancellationToken);

        List<string> changes = [];
        if (name != chat.Name)
            changes.Add($"renamed it \"{name}\"");
        if (audience != chat.Audience)
            changes.Add($"made it for {AudienceWords(audience)}");
        if (changes.Count == 0)
            return;
        chat.Name = name;
        chat.Audience = audience;
        audit.Record(me, AuditActions.Changed, string.Join(" and ", changes), chat.Id);
        marks.Touch(chat);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(chat.Id, [], true, cancellationToken);
    }

    public async Task DeleteAsync(Guid me, ChatRef request, CancellationToken cancellationToken)
    {
        ChatRecord chat = await ManagedAsync(me, request.ChatId, cancellationToken);
        chat.DeletedAt = time.GetUtcNow().UtcDateTime;
        chat.DeletedBy = me;
        audit.Record(me, AuditActions.Deleted, $"Deleted \"{chat.Name}\". Its messages are kept for checking", chat.Id);
        marks.Touch(chat);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(chat.Id, [], true, cancellationToken);
    }

    public async Task ChangePeopleAsync(Guid me, ChatPeopleChange request, CancellationToken cancellationToken)
    {
        ChatRecord chat = await ManagedAsync(me, request.ChatId, cancellationToken);
        if (!ChatAudienceRules.UsesChosenPeople(chat))
            throw ChatErrors.Invalid("This chat is for everyone in its audience. Change who it's for to choose people.");

        List<Guid> adding = request.Add.Select(text => ChatErrors.IdOf(text, "person")).Distinct().ToList();
        List<Guid> removing = request.Remove.Select(text => ChatErrors.IdOf(text, "person")).Distinct().Where(person => person != chat.AskerId).ToList();
        if (adding.Count + removing.Count == 0)
            return;
        foreach (Guid person in adding)
            await roles.GiveAsFeatureAsync(person, ChatsPermissions.MemberRole, chat.Id.ToString(), null, cancellationToken);
        foreach (Guid person in removing)
            await roles.TakeAwayAsFeatureAsync(person, ChatsPermissions.MemberRole, chat.Id.ToString(), cancellationToken);
        facts.Forget();

        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync([me, .. adding, .. removing], cancellationToken);
        string actor = names.GetValueOrDefault(me, "A leader");
        List<string> said = [];
        if (adding.Count > 0)
            said.Add($"{actor} added {Join(adding.Select(person => names.GetValueOrDefault(person, "someone")))}.");
        if (removing.Count > 0)
            said.Add($"{actor} took {Join(removing.Select(person => names.GetValueOrDefault(person, "someone")))} out of the chat.");
        audit.Record(me, AuditActions.PeopleChanged, string.Join(" ", said), chat.Id);
        MessageRecord note = await AddMessageAsync(chat, me, null, MessageKinds.Event, $"people:{Guid.NewGuid():N}", string.Join(" ", said), null, [], [], cancellationToken);
        Published published = await publisher.PublishAsync(chat.Id, [note.Id], true, cancellationToken);
        await notifier.ConsentAsync(published, me, cancellationToken);
    }

    public async Task SendAsync(Guid me, SendMessage request, CancellationToken cancellationToken, bool opensAsk = false)
    {
        Guid chatId = ChatErrors.IdOf(request.ChatId, "chat");
        string clientId = request.ClientId.Length is > 0 and <= 80 ? request.ClientId : throw ChatErrors.Invalid("That message has no id.");
        if (await db.Messages.AnyAsync(message => message.SenderId == me && message.ClientId == clientId, cancellationToken))
            return;
        if ((await facts.PausedAsync(cancellationToken)).Contains(me))
            throw ChatErrors.Refused(ChatErrors.Paused);

        ChatRecord chat = await OpenChatAsync(chatId, cancellationToken);
        Published plan = await publisher.PlanAsync(chat, cancellationToken);
        Guid? forPerson = null;
        if (request.ForPersonId.Length > 0)
        {
            Guid child = ChatErrors.IdOf(request.ForPersonId, "person");
            if (!ChatMembership.MaySendFor(plan.Plan, chat.Kind, me, child) || !await families.MayActForAsync(me, child, cancellationToken))
                throw ChatErrors.Refused("You can't write for them in this chat.");
            forPerson = child;
        }
        else if (!ChatMembership.MaySend(plan.Plan, chat.Kind, me) && !(opensAsk && chat.Kind == ChatKinds.Ask && chat.AskerId == me))
        {
            throw ChatErrors.Refused(plan.Plan.Of(me) switch
            {
                null => ChatErrors.NotInChat,
                { State: ChatStates.AwaitingConsent } => "A parent needs to say yes before you can join this chat.",
                { Role: ChatRoles.ParentReader } => "Parents can read this chat. To write, reply for your child.",
                _ => "Only leaders post in this channel.",
            });
        }

        string text = request.Text.Trim();
        if (text.Length > ChatLimits.TextLength)
            throw ChatErrors.Invalid($"Keep a message to {ChatLimits.TextLength} characters.");
        if (RichTextRules.ChatProblems(text) is [var linkProblem, ..])
            throw ChatErrors.Invalid(linkProblem);
        if (request.Attachments.Count > ChatLimits.AttachmentsPerMessage)
            throw ChatErrors.Invalid($"Send up to {ChatLimits.AttachmentsPerMessage} photos or files at a time.");
        if (text.Length == 0 && request.Attachments.Count == 0)
            throw ChatErrors.Invalid("Write a message first.");

        HashSet<Guid> inChat = plan.Plan.Participants.Select(participant => participant.PersonId).ToHashSet();
        List<Guid> mentions = request.Mentions.Select(mention => Guid.TryParse(mention, out Guid id) ? id : Guid.Empty).Where(inChat.Contains).Distinct().ToList();
        Guid? replyTo = null;
        if (request.ReplyToId.Length > 0)
        {
            Guid reply = ChatErrors.IdOf(request.ReplyToId, "message");
            replyTo = await db.Messages.AnyAsync(message => message.Id == reply && message.ChatId == chatId, cancellationToken)
                ? reply
                : throw ChatErrors.Gone("The message you replied to isn't in this chat.");
        }

        List<StoredAttachment> attachments = await media.VerifyAsync(request.Attachments, cancellationToken);
        MessageRecord message = await AddMessageAsync(chat, me, forPerson, MessageKinds.Text, clientId, text, replyTo, mentions, attachments, cancellationToken);
        await media.KeepAsync(message, chat.Name, cancellationToken);
        Published published = await publisher.PublishAsync(chatId, [message.Id], true, cancellationToken);
        await notifier.MessageAsync(published, message, cancellationToken);
        await notifier.ConsentAsync(published, me, cancellationToken);
    }

    public async Task EditAsync(Guid me, EditMessage request, CancellationToken cancellationToken)
    {
        MessageRecord message = await OwnMessageAsync(me, request.MessageId, cancellationToken);
        string text = request.Text.Trim();
        if (text.Length == 0 && message.Attachments.Count == 0)
            throw ChatErrors.Invalid("Write something, or delete the message instead.");
        if (text.Length > ChatLimits.TextLength)
            throw ChatErrors.Invalid($"Keep a message to {ChatLimits.TextLength} characters.");
        if (RichTextRules.ChatProblems(text) is [var linkProblem, ..])
            throw ChatErrors.Invalid(linkProblem);
        if (text == message.Text)
            return;

        DateTime now = time.GetUtcNow().UtcDateTime;
        message.Text = text;
        message.EditedAt = now;
        await AddVersionAsync(message, AuditActions.Edited, me, cancellationToken);
        audit.Record(me, AuditActions.Edited, "Edited a message", message.ChatId, message.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(message.ChatId, [message.Id], true, cancellationToken);
    }

    public async Task DeleteMessageAsync(Guid me, MessageRef request, CancellationToken cancellationToken)
    {
        Guid messageId = ChatErrors.IdOf(request.MessageId, "message");
        MessageRecord message = await db.Messages.SingleOrDefaultAsync(found => found.Id == messageId, cancellationToken)
                                ?? throw ChatErrors.Gone("That message isn't here any more.");
        if (message.DeletedAt is not null)
            return;
        ChatRecord chat = await OpenChatAsync(message.ChatId, cancellationToken);
        bool leader = await facts.IsAllowedAsync(me, ChatsPermissions.Manage, chat.MinistryId.ToString(), cancellationToken);
        if (message.SenderId != me && !leader)
            throw ChatErrors.Refused("You can delete your own messages. A leader can delete any message.");
        if (message.Kind != MessageKinds.Text)
            throw ChatErrors.Invalid("That line can't be deleted.");

        message.DeletedAt = time.GetUtcNow().UtcDateTime;
        message.DeletedBy = me;
        await AddVersionAsync(message, AuditActions.MessageDeleted, me, cancellationToken);
        audit.Record(me, AuditActions.MessageDeleted, message.SenderId == me ? "Deleted their message" : "A leader deleted a message", chat.Id, message.Id);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(chat.Id, [message.Id], true, cancellationToken);
    }

    public async Task ReactAsync(Guid me, React request, CancellationToken cancellationToken)
    {
        Guid messageId = ChatErrors.IdOf(request.MessageId, "message");
        MessageRecord message = await db.Messages.AsNoTracking().SingleOrDefaultAsync(found => found.Id == messageId, cancellationToken)
                                ?? throw ChatErrors.Gone("That message isn't here any more.");
        string emoji = request.Emoji.Trim();
        if (emoji.Length is 0 or > ChatLimits.ReactionLength)
            throw ChatErrors.Invalid("Choose a reaction.");
        await WriterAsync(me, message.ChatId, cancellationToken);

        ReactionRecord? existing = await db.Reactions.FindAsync([messageId, me, emoji], cancellationToken);
        if (request.On == (existing is not null))
            return;
        if (existing is null)
            db.Reactions.Add(new ReactionRecord { MessageId = messageId, PersonId = me, Emoji = emoji, At = time.GetUtcNow().UtcDateTime });
        else
            db.Reactions.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(message.ChatId, [messageId], false, cancellationToken);
    }

    public async Task MarkReadAsync(Guid me, MarkRead request, CancellationToken cancellationToken)
    {
        Guid chatId = ChatErrors.IdOf(request.ChatId, "chat");
        ParticipantRecord? participant = await db.Participants.FindAsync([chatId, me], cancellationToken);
        ChatRecord? chat = await db.Chats.FindAsync([chatId], cancellationToken);
        if (participant is null || chat is null || participant.State != ChatStates.Active)
            return;
        long upTo = Math.Min(request.UpToSeq, chat.LastSeq);
        if (upTo <= participant.ReadSeq)
            return;
        participant.ReadSeq = upTo;
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(chatId, [], true, cancellationToken);
    }

    public async Task MuteAsync(Guid me, Mute request, CancellationToken cancellationToken)
    {
        Guid chatId = ChatErrors.IdOf(request.ChatId, "chat");
        ParticipantRecord participant = await db.Participants.FindAsync([chatId, me], cancellationToken) ?? throw ChatErrors.Refused(ChatErrors.NotInChat);
        if (participant.Muted == request.Muted)
            return;
        participant.Muted = request.Muted;
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(chatId, [], true, cancellationToken);
    }

    public async Task ConsentAsync(Guid me, Consent request, CancellationToken cancellationToken)
    {
        Guid chatId = ChatErrors.IdOf(request.ChatId, "chat");
        Guid child = ChatErrors.IdOf(request.ChildId, "person");
        ChatRecord chat = await OpenChatAsync(chatId, cancellationToken);
        IReadOnlyDictionary<Guid, FamilyFacts> family = await families.FactsAsync([child], cancellationToken);
        if (family.GetValueOrDefault(child) is not { } childFacts || !childFacts.Parents.Contains(me))
            throw ChatErrors.Refused("Only a parent of theirs can decide this.");
        Published plan = await publisher.PlanAsync(chat, cancellationToken);
        if (plan.Plan.Of(child) is null)
            throw ChatErrors.Gone("They aren't in this chat any more.");

        ConsentRecord? consent = await db.Consents.FindAsync([chatId, child], cancellationToken);
        if (consent is null)
            db.Consents.Add(consent = new ConsentRecord { ChatId = chatId, ChildId = child });
        if (consent.Approved == request.Approve)
            return;
        consent.Approved = request.Approve;
        consent.DecidedBy = me;
        consent.DecidedAt = time.GetUtcNow().UtcDateTime;
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync([child], cancellationToken);
        string name = names.GetValueOrDefault(child, "their child");
        audit.Record(me, AuditActions.Consent, request.Approve ? $"Said yes to {name} joining" : $"Said not now to {name} joining", chatId, person: child);
        marks.Touch(chat);
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAsync(chatId, [], true, cancellationToken);
        await notifier.SettleAsync(ChatNotifier.ConsentSubject(chatId, child), cancellationToken);
    }

    public async Task ReportAsync(Guid me, ReportMessage request, CancellationToken cancellationToken)
    {
        Guid reportId = ChatErrors.IdOf(request.ReportId, "report");
        if (await db.Reports.AnyAsync(report => report.Id == reportId, cancellationToken))
            return;
        Guid messageId = ChatErrors.IdOf(request.MessageId, "message");
        MessageRecord message = await db.Messages.AsNoTracking().SingleOrDefaultAsync(found => found.Id == messageId, cancellationToken)
                                ?? throw ChatErrors.Gone("That message isn't here any more.");
        if (!ReportRoutes.IsValid(request.To))
            throw ChatErrors.Invalid("Choose who to tell.");
        string note = request.Note.Trim();
        if (note.Length > ChatLimits.NoteLength)
            throw ChatErrors.Invalid($"Keep the note to {ChatLimits.NoteLength} characters.");
        ParticipantRecord? participant = await db.Participants.AsNoTracking()
            .SingleOrDefaultAsync(found => found.ChatId == message.ChatId && found.PersonId == me, cancellationToken);
        if (participant is not { State: ChatStates.Active })
            throw ChatErrors.Refused(ChatErrors.NotInChat);

        ChatRecord chat = await db.Chats.SingleAsync(found => found.Id == message.ChatId, cancellationToken);
        ReportRecord report = new()
        {
            Id = reportId, MessageId = messageId, ChatId = chat.Id, ReporterId = me, To = request.To, Note = note, At = time.GetUtcNow().UtcDateTime,
        };
        db.Reports.Add(report);
        audit.Record(me, AuditActions.Reported,
            request.To == ReportRoutes.Auditors ? "Reported a message to the church's chat auditors" : "Reported a message to the ministry's leaders", chat.Id, messageId);
        marks.Touch(chat);
        await db.SaveChangesAsync(cancellationToken);
        ChatPlace? place = await facts.PlaceAsync(chat.MinistryId, cancellationToken);
        await notifier.ReportAsync(report, chat, place?.Name ?? "a ministry", cancellationToken);
    }

    public async Task AskToJoinAsync(Guid me, AskToJoin request, CancellationToken cancellationToken)
    {
        Guid chatId = ChatErrors.IdOf(request.ChatId, "chat");
        if (await db.Chats.AnyAsync(chat => chat.Id == chatId, cancellationToken))
            return;
        if ((await facts.PausedAsync(cancellationToken)).Contains(me))
            throw ChatErrors.Refused(ChatErrors.Paused);
        Guid ministryId = ChatErrors.IdOf(request.MinistryId, "ministry");
        ChatPlace place = await facts.PlaceAsync(ministryId, cancellationToken) ?? throw ChatErrors.Gone("That ministry was deleted.");
        if (!place.ChatsOn)
            throw ChatErrors.Refused($"{place.Name} doesn't use chats. Ask one of its leaders in person.");
        Guid asker = me;
        if (request.ForPersonId.Length > 0)
        {
            asker = ChatErrors.IdOf(request.ForPersonId, "person");
            if (asker != me && !await families.MayActForAsync(me, asker, cancellationToken))
                throw ChatErrors.Refused("You can ask for yourself or for your own children.");
        }

        ChatRecord? open = await db.Chats.FirstOrDefaultAsync(chat =>
            chat.MinistryId == ministryId && chat.Kind == ChatKinds.Ask && chat.AskerId == asker && chat.DeletedAt == null, cancellationToken);
        if (open is null)
        {
            IReadOnlyDictionary<Guid, string> names = await people.NamesAsync([asker], cancellationToken);
            open = new ChatRecord
            {
                Id = chatId, MinistryId = ministryId, Kind = ChatKinds.Ask, Audience = ChatAudiences.Chosen, AskerId = asker, CreatedBy = me,
                AskedAs = request.Place is "team" or "attend" ? request.Place : "",
                CreatedAt = time.GetUtcNow().UtcDateTime, Name = NameOf($"{names.GetValueOrDefault(asker, "Someone")} about joining"),
            };
            db.Chats.Add(open);
            audit.Record(me, AuditActions.Started, $"Asked to join {place.Name}", open.Id, person: asker);
            marks.Touch(open);
            await db.SaveChangesAsync(cancellationToken);
            foreach (Guid person in new[] { me, asker }.Distinct())
                await roles.GiveAsFeatureAsync(person, ChatsPermissions.MemberRole, open.Id.ToString(), null, cancellationToken);
        }

        string text = request.Text.Trim().Length > 0 ? request.Text : ChatCopy.AskToJoin(place.Name);
        await SendAsync(me, new SendMessage { ChatId = open.Id.ToString(), ClientId = $"ask:{chatId:N}", Text = text }, cancellationToken, opensAsk: true);
    }

    private async Task<MessageRecord> AddMessageAsync(
        ChatRecord chat, Guid sender, Guid? forPerson, string kind, string clientId, string text, Guid? replyTo, List<Guid> mentions,
        List<StoredAttachment> attachments, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT 1 FROM chats.chats WHERE id = {chat.Id} FOR UPDATE", cancellationToken);
        await db.Entry(chat).ReloadAsync(cancellationToken);
        chat.LastSeq++;
        MessageRecord message = new()
        {
            Id = Guid.NewGuid(), ChatId = chat.Id, Seq = chat.LastSeq, Kind = kind, SenderId = sender, ForPersonId = forPerson, ClientId = clientId,
            SentAt = time.GetUtcNow().UtcDateTime, Text = text, ReplyToId = replyTo, Mentions = mentions, Attachments = attachments,
        };
        db.Messages.Add(message);
        if (await db.Participants.FindAsync([chat.Id, sender], cancellationToken) is { } participant)
            participant.ReadSeq = chat.LastSeq;
        await AddVersionAsync(message, AuditActions.Sent, sender, cancellationToken);
        if (kind == MessageKinds.Text)
            audit.Record(sender, AuditActions.Sent, forPerson is null ? "Sent a message" : "Sent a message for their child", chat.Id, message.Id);
        marks.Touch(chat);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return message;
    }

    private async Task AddVersionAsync(MessageRecord message, string action, Guid by, CancellationToken cancellationToken)
    {
        int number = await db.Versions.Where(version => version.MessageId == message.Id).CountAsync(cancellationToken)
                     + db.Versions.Local.Count(version => version.MessageId == message.Id) + 1;
        db.Versions.Add(new MessageVersionRecord
        {
            Id = Guid.NewGuid(), MessageId = message.Id, Number = number, Action = action, Text = message.Text, Attachments = message.Attachments.ToList(),
            At = time.GetUtcNow().UtcDateTime, ById = by,
        });
    }

    private async Task<MessageRecord> OwnMessageAsync(Guid me, string id, CancellationToken cancellationToken)
    {
        Guid messageId = ChatErrors.IdOf(id, "message");
        MessageRecord message = await db.Messages.SingleOrDefaultAsync(found => found.Id == messageId, cancellationToken)
                                ?? throw ChatErrors.Gone("That message isn't here any more.");
        if (message.SenderId != me || message.Kind != MessageKinds.Text)
            throw ChatErrors.Refused("You can only change your own messages.");
        if (message.DeletedAt is not null)
            throw ChatErrors.Gone("That message was deleted.");
        await WriterAsync(me, message.ChatId, cancellationToken);
        return message;
    }

    private async Task WriterAsync(Guid me, Guid chatId, CancellationToken cancellationToken)
    {
        if ((await facts.PausedAsync(cancellationToken)).Contains(me))
            throw ChatErrors.Refused(ChatErrors.Paused);
        ChatRecord chat = await OpenChatAsync(chatId, cancellationToken);
        Published plan = await publisher.PlanAsync(chat, cancellationToken);
        if (plan.Plan.Of(me) is not { Receives: true })
            throw ChatErrors.Refused(ChatErrors.NotInChat);
    }

    private async Task<ChatRecord> ManagedAsync(Guid me, string id, CancellationToken cancellationToken)
    {
        ChatRecord chat = await OpenChatAsync(ChatErrors.IdOf(id, "chat"), cancellationToken);
        if (!await facts.IsAllowedAsync(me, ChatsPermissions.Manage, chat.MinistryId.ToString(), cancellationToken))
            throw ChatErrors.Refused("Only the ministry's leaders can change its chats.");
        return chat;
    }

    private async Task<ChatRecord> OpenChatAsync(Guid chatId, CancellationToken cancellationToken)
    {
        ChatRecord chat = await db.Chats.SingleOrDefaultAsync(found => found.Id == chatId, cancellationToken) ?? throw ChatErrors.Gone(ChatErrors.ChatGone);
        return chat.DeletedAt is null ? chat : throw ChatErrors.Gone(ChatErrors.ChatGone);
    }

    private static string NameOf(string name)
    {
        string trimmed = name.Trim();
        return trimmed.Length switch
        {
            0 => throw ChatErrors.Invalid("Give the chat a name."),
            > ChatLimits.NameLength => throw ChatErrors.Invalid($"Keep the name to {ChatLimits.NameLength} characters."),
            _ => trimmed,
        };
    }

    internal static string AudienceWords(string audience) => audience switch
    {
        ChatAudiences.Team => "the team",
        ChatAudiences.Attendees => "attendees and their parents",
        ChatAudiences.Parents => "parents",
        _ => "chosen people",
    };

    private static string Join(IEnumerable<string> names)
    {
        List<string> list = names.ToList();
        return list.Count <= 1 ? string.Concat(list) : $"{string.Join(", ", list[..^1])} and {list[^1]}";
    }
}
