using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Chats;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatOversight(
    ChatsDbContext db, ChatFacts facts, ChatPublisher publisher, ChatNotifier notifier, ChatAudit audit, OversightMarks marks,
    IChatPlaces places, IAccessEvaluator access, IRoleAssignments roles, IPeopleIndex people, TimeProvider time)
{
    private const int LogLines = 200;

    public async Task<Desk> DeskAsync(Guid me, CancellationToken cancellationToken)
    {
        GrantSet grants = await access.GrantsForAsync(me, cancellationToken);
        bool auditor = grants.IsAllowed(ChatsPermissions.Audit);
        IReadOnlyList<ChatPlace> all = await places.AllAsync(cancellationToken);
        Dictionary<Guid, ChatPlace> byId = all.ToDictionary(place => place.MinistryId);
        HashSet<Guid> checkable = all.Where(place => auditor || grants.IsAllowed(ChatsPermissions.Check, place.MinistryId.ToString()))
            .Select(place => place.MinistryId).ToHashSet();

        List<ChatRecord> chats = await db.Chats.AsNoTracking().Where(chat => chat.DeletedAt == null).ToListAsync(cancellationToken);
        chats = chats.Where(chat => checkable.Contains(chat.MinistryId)).ToList();
        List<Guid> ids = chats.Select(chat => chat.Id).ToList();
        Dictionary<Guid, int> receiving = await db.Participants.AsNoTracking()
            .Where(participant => ids.Contains(participant.ChatId) && participant.State == ChatStates.Active && participant.Role != ChatRoles.ParentReader)
            .GroupBy(participant => participant.ChatId).Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);
        Dictionary<Guid, (int Count, DateTime Last)> messages = (await db.Messages.AsNoTracking()
                .Where(message => ids.Contains(message.ChatId) && message.Kind == MessageKinds.Text)
                .GroupBy(message => message.ChatId).Select(group => new { group.Key, Count = group.Count(), Last = group.Max(message => message.SentAt) })
                .ToListAsync(cancellationToken))
            .ToDictionary(group => group.Key, group => (group.Count, group.Last));
        Dictionary<Guid, int> reports = await OpenReportsAsync(ids, cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await FlagNamesAsync(chats, cancellationToken);

        return new Desk
        {
            Auditor = auditor,
            OpenReports = reports.Values.Sum(),
            Chats = chats
                .OrderBy(chat => byId.GetValueOrDefault(chat.MinistryId)?.Name)
                .ThenByDescending(chat => messages.GetValueOrDefault(chat.Id).Last)
                .Select(chat => new DeskChat
                {
                    Id = chat.Id.ToString(), MinistryId = chat.MinistryId.ToString(), MinistryName = byId.GetValueOrDefault(chat.MinistryId)?.Name ?? "",
                    Name = chat.Name, Kind = chat.Kind, Audience = chat.Audience, People = receiving.GetValueOrDefault(chat.Id),
                    Messages = messages.GetValueOrDefault(chat.Id).Count,
                    LastAt = messages.TryGetValue(chat.Id, out (int Count, DateTime Last) found) ? found.Last : null,
                    Flags = chat.Flags.Select(flag => Flagged.Words(flag, names)).ToList(),
                    MayManage = grants.IsAllowed(ChatsPermissions.Manage, chat.MinistryId.ToString()),
                    OpenReports = reports.GetValueOrDefault(chat.Id),
                    HasMinors = chat.HasMinors,
                })
                .ToList(),
            Ministries = all
                .Where(place => grants.IsAllowed(ChatsPermissions.Manage, place.MinistryId.ToString()) || checkable.Contains(place.MinistryId))
                .Select(place => new DeskMinistry
                {
                    Id = place.MinistryId.ToString(), Name = place.Name, ChatsOn = place.ChatsOn,
                    MayManage = grants.IsAllowed(ChatsPermissions.Manage, place.MinistryId.ToString()),
                    Audiences = [.. place.Audiences.Keys, ChatAudiences.Chosen],
                })
                .ToList(),
        };
    }

    public async Task<CheckedChat> CheckAsync(Guid me, CheckRequest request, CancellationToken cancellationToken)
    {
        ChatRecord chat = await CheckableAsync(me, request.ChatId, cancellationToken);
        string reason = request.Reason.Trim();
        if (reason.Length == 0)
            throw ChatErrors.Invalid("Say why you're checking this chat. It's kept in the log.");
        if (reason.Length > ChatLimits.NoteLength)
            throw ChatErrors.Invalid($"Keep the reason to {ChatLimits.NoteLength} characters.");

        audit.Record(me, AuditActions.Checked, reason, chat.Id);
        marks.Touch(chat);
        await db.SaveChangesAsync(cancellationToken);

        Published published = await publisher.PlanAsync(chat, cancellationToken);
        List<MessageRecord> messages = await db.Messages.AsNoTracking().Where(message => message.ChatId == chat.Id).OrderBy(message => message.Seq)
            .ToListAsync(cancellationToken);
        List<Guid> ids = messages.Select(message => message.Id).ToList();
        ILookup<Guid, MessageVersionRecord> versions = (await db.Versions.AsNoTracking().Where(version => ids.Contains(version.MessageId))
            .OrderBy(version => version.Number).ToListAsync(cancellationToken)).ToLookup(version => version.MessageId);
        ILookup<Guid, ReactionRecord> reactions = (await db.Reactions.AsNoTracking().Where(reaction => ids.Contains(reaction.MessageId))
            .ToListAsync(cancellationToken)).ToLookup(reaction => reaction.MessageId);
        Dictionary<Guid, MessageRecord> byId = messages.ToDictionary(message => message.Id);
        HashSet<Guid> named = published.Plan.Participants.Select(participant => participant.PersonId).ToHashSet();
        named.UnionWith(messages.SelectMany(message => new[] { message.SenderId, message.ForPersonId, message.DeletedBy }).OfType<Guid>());
        named.UnionWith(versions.SelectMany(group => group).Select(version => version.ById).OfType<Guid>());
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(named, cancellationToken);
        DeskChat line = (await DeskAsync(me, cancellationToken)).Chats.FirstOrDefault(found => found.Id == chat.Id.ToString())
                        ?? new DeskChat { Id = chat.Id.ToString(), Name = chat.Name };

        return new CheckedChat
        {
            Chat = line,
            People = ChatCards.For(published, new Participant(me, ChatRoles.Leader, ChatStates.Active, []),
                await db.Participants.AsNoTracking().Where(participant => participant.ChatId == chat.Id).ToDictionaryAsync(participant => participant.PersonId, cancellationToken),
                names, null).People,
            Messages = messages.Select(message => new CheckedMessage
            {
                Message = ChatRows.MessageView(message, names, reactions[message.Id].ToList(), message.ReplyToId is { } reply ? byId.GetValueOrDefault(reply) : null),
                DeletedText = message.DeletedAt is null ? "" : message.Text,
                Versions = versions[message.Id].Select(version => new MessageVersion
                {
                    Number = version.Number, Action = version.Action, Text = version.Text, At = version.At, ByName = ChatRows.NameOf(version.ById, names),
                    Attachments = version.Attachments.Select(ChatRows.ToAttachment).ToList(),
                }).ToList(),
            }).ToList(),
            Checks = (await LinesAsync(db.Audit.AsNoTracking().Where(entry => entry.ChatId == chat.Id && entry.Action == AuditActions.Checked), cancellationToken)),
        };
    }

    public async Task<Log> LogAsync(Guid me, LogRequest request, CancellationToken cancellationToken)
    {
        IQueryable<AuditRecord> entries = db.Audit.AsNoTracking();
        if (request.ChatId.Length > 0)
        {
            ChatRecord chat = await CheckableAsync(me, request.ChatId, cancellationToken, includeDeleted: true);
            entries = entries.Where(entry => entry.ChatId == chat.Id);
        }
        else
        {
            GrantSet grants = await access.GrantsForAsync(me, cancellationToken);
            if (!grants.IsAllowed(ChatsPermissions.Audit))
            {
                List<Guid> mine = await CheckableChatIdsAsync(grants, cancellationToken);
                entries = entries.Where(entry => entry.ChatId != null && mine.Contains(entry.ChatId.Value));
            }
        }
        int limit = request.Limit is > 0 and <= LogLines ? request.Limit : LogLines;
        return new Log { Lines = await LinesAsync(entries.OrderByDescending(entry => entry.At).Take(limit), cancellationToken) };
    }

    public async Task<Reports> ReportsAsync(Guid me, CancellationToken cancellationToken)
    {
        GrantSet grants = await access.GrantsForAsync(me, cancellationToken);
        IQueryable<ReportRecord> reports = db.Reports.AsNoTracking();
        if (!grants.IsAllowed(ChatsPermissions.Audit))
        {
            List<Guid> mine = await CheckableChatIdsAsync(grants, cancellationToken);
            reports = reports.Where(report => report.To == ReportRoutes.Leaders && mine.Contains(report.ChatId));
        }
        List<ReportRecord> all = await reports.OrderByDescending(report => report.At).Take(LogLines).ToListAsync(cancellationToken);
        List<Guid> messageIds = all.Select(report => report.MessageId).ToList();
        Dictionary<Guid, MessageRecord> messages = await db.Messages.AsNoTracking().Where(message => messageIds.Contains(message.Id))
            .ToDictionaryAsync(message => message.Id, cancellationToken);
        List<Guid> chatIds = all.Select(report => report.ChatId).Distinct().ToList();
        Dictionary<Guid, ChatRecord> chats = await db.Chats.AsNoTracking().Where(chat => chatIds.Contains(chat.Id)).ToDictionaryAsync(chat => chat.Id, cancellationToken);
        IReadOnlyDictionary<Guid, ChatPlace> ministries = await places.PlacesAsync(chats.Values.Select(chat => chat.MinistryId).Distinct().ToList(), cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(
            all.SelectMany(report => new[] { report.ReporterId, report.HandledBy, messages.GetValueOrDefault(report.MessageId)?.SenderId }).OfType<Guid>().ToList(),
            cancellationToken);

        List<ReportLine> lines = all.Select(report =>
        {
            MessageRecord? message = messages.GetValueOrDefault(report.MessageId);
            ChatRecord? chat = chats.GetValueOrDefault(report.ChatId);
            return new ReportLine
            {
                Id = report.Id.ToString(), ChatId = report.ChatId.ToString(), ChatName = chat?.Name ?? "", To = report.To,
                MinistryName = chat is null ? "" : ministries.GetValueOrDefault(chat.MinistryId)?.Name ?? "",
                ReporterName = ChatRows.NameOf(report.ReporterId, names), Note = report.Note, At = report.At, MessageId = report.MessageId.ToString(),
                MessageSender = message is null ? "" : ChatRows.NameOf(message.SenderId, names), MessageSenderId = message?.SenderId?.ToString() ?? "",
                MessageText = message?.Text ?? "", HandledAt = report.HandledAt, HandledBy = report.HandledBy is null ? "" : ChatRows.NameOf(report.HandledBy, names),
                Outcome = report.Outcome,
            };
        }).ToList();
        return new Reports { Open = lines.Where(line => line.HandledAt is null).ToList(), Handled = lines.Where(line => line.HandledAt is not null).ToList() };
    }

    public async Task HandleAsync(Guid me, HandleReport request, CancellationToken cancellationToken)
    {
        Guid id = ChatErrors.IdOf(request.ReportId, "report");
        ReportRecord report = await db.Reports.SingleOrDefaultAsync(found => found.Id == id, cancellationToken) ?? throw ChatErrors.Gone("That report isn't here any more.");
        GrantSet grants = await access.GrantsForAsync(me, cancellationToken);
        ChatRecord chat = await db.Chats.SingleAsync(found => found.Id == report.ChatId, cancellationToken);
        bool may = grants.IsAllowed(ChatsPermissions.Audit)
                   || (report.To == ReportRoutes.Leaders && grants.IsAllowed(ChatsPermissions.Check, chat.MinistryId.ToString()));
        if (!may)
            throw ChatErrors.Refused("Only the people this report went to can close it.");
        string outcome = request.Outcome.Trim();
        if (outcome.Length == 0)
            throw ChatErrors.Invalid("Say what was done. It's kept with the report.");
        if (outcome.Length > ChatLimits.NoteLength)
            throw ChatErrors.Invalid($"Keep it to {ChatLimits.NoteLength} characters.");
        if (report.HandledAt is not null)
            return;
        report.HandledAt = time.GetUtcNow().UtcDateTime;
        report.HandledBy = me;
        report.Outcome = outcome;
        audit.Record(me, AuditActions.Handled, outcome, chat.Id, report.MessageId);
        marks.Touch(chat);
        await db.SaveChangesAsync(cancellationToken);
        await notifier.SettleAsync(ChatNotifier.ReportSubject(report.Id), cancellationToken);
    }

    public async Task<PausedList> PausedAsync(CancellationToken cancellationToken)
    {
        IReadOnlySet<Guid> paused = await facts.PausedAsync(cancellationToken);
        List<AuditRecord> pauses = await db.Audit.AsNoTracking()
            .Where(entry => entry.Action == AuditActions.Paused && entry.PersonId != null && paused.Contains(entry.PersonId.Value))
            .ToListAsync(cancellationToken);
        Dictionary<Guid, AuditRecord> latest = pauses.GroupBy(entry => entry.PersonId!.Value).ToDictionary(group => group.Key, group => group.MaxBy(entry => entry.At)!);
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(paused.Concat(pauses.Select(entry => entry.ActorId).OfType<Guid>()).ToList(), cancellationToken);
        return new PausedList
        {
            People = paused.Select(person => new PausedPerson
            {
                Id = person.ToString(), Name = names.GetValueOrDefault(person, "Someone"), Since = latest.GetValueOrDefault(person)?.At,
                Reason = latest.GetValueOrDefault(person)?.Words ?? "", ByName = ChatRows.NameOf(latest.GetValueOrDefault(person)?.ActorId, names),
            }).OrderBy(person => person.Name).ToList(),
        };
    }

    public async Task PauseAsync(Guid me, PauseRequest request, CancellationToken cancellationToken)
    {
        Guid person = ChatErrors.IdOf(request.PersonId, "person");
        string reason = request.Reason.Trim();
        if (request.Paused && reason.Length == 0)
            throw ChatErrors.Invalid("Say why. It's kept in the log.");
        if (reason.Length > ChatLimits.NoteLength)
            throw ChatErrors.Invalid($"Keep the reason to {ChatLimits.NoteLength} characters.");
        bool already = (await facts.PausedAsync(cancellationToken)).Contains(person);
        if (already == request.Paused)
            return;

        if (request.Paused)
            await roles.GiveAsFeatureAsync(person, ChatsPermissions.PausedRole, null, null, cancellationToken);
        else
            await roles.TakeAwayAsFeatureAsync(person, ChatsPermissions.PausedRole, null, cancellationToken);
        facts.Forget();
        audit.Record(me, request.Paused ? AuditActions.Paused : AuditActions.Unpaused, reason.Length > 0 ? reason : "Can use chats again", person: person);
        DateTime now = time.GetUtcNow().UtcDateTime;
        db.FeedChanges.Add(ChatRows.Status(person, new ChatsStatus { Paused = request.Paused, PausedAt = request.Paused ? now : null }, now));
        await db.SaveChangesAsync(cancellationToken);
        await publisher.PublishAllAsync(cancellationToken);
    }

    private async Task<ChatRecord> CheckableAsync(Guid me, string id, CancellationToken cancellationToken, bool includeDeleted = false)
    {
        Guid chatId = ChatErrors.IdOf(id, "chat");
        ChatRecord chat = await db.Chats.AsNoTracking().SingleOrDefaultAsync(found => found.Id == chatId, cancellationToken) ?? throw ChatErrors.Gone(ChatErrors.ChatGone);
        if (chat.DeletedAt is not null && !includeDeleted && !await facts.IsAllowedAsync(me, ChatsPermissions.Audit, null, cancellationToken))
            throw ChatErrors.Gone(ChatErrors.ChatGone);
        GrantSet grants = await access.GrantsForAsync(me, cancellationToken);
        return MayCheck(grants, chat) ? chat : throw ChatErrors.Refused("Only the ministry's leaders and the church's chat auditors can check this chat.");
    }

    internal static bool MayCheck(GrantSet grants, ChatRecord chat) =>
        grants.IsAllowed(ChatsPermissions.Audit) || grants.IsAllowed(ChatsPermissions.Check, chat.MinistryId.ToString());

    private async Task<List<Guid>> CheckableChatIdsAsync(GrantSet grants, CancellationToken cancellationToken) =>
        (await db.Chats.AsNoTracking().ToListAsync(cancellationToken)).Where(chat => MayCheck(grants, chat)).Select(chat => chat.Id).ToList();

    private async Task<Dictionary<Guid, int>> OpenReportsAsync(List<Guid> chats, CancellationToken cancellationToken) =>
        await db.Reports.AsNoTracking().Where(report => chats.Contains(report.ChatId) && report.HandledAt == null)
            .GroupBy(report => report.ChatId).Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.Key, group => group.Count, cancellationToken);

    private async Task<IReadOnlyDictionary<Guid, string>> FlagNamesAsync(List<ChatRecord> chats, CancellationToken cancellationToken)
    {
        List<Guid> ids = chats.SelectMany(chat => chat.Flags)
            .Where(flag => flag.StartsWith(Flagged.NoParentPrefix, StringComparison.Ordinal))
            .Select(flag => Guid.TryParse(flag[Flagged.NoParentPrefix.Length..], out Guid id) ? id : Guid.Empty)
            .ToList();
        return await people.NamesAsync(ids, cancellationToken);
    }

    private async Task<List<LogLine>> LinesAsync(IQueryable<AuditRecord> query, CancellationToken cancellationToken)
    {
        List<AuditRecord> entries = await query.ToListAsync(cancellationToken);
        List<Guid> chatIds = entries.Select(entry => entry.ChatId).OfType<Guid>().Distinct().ToList();
        Dictionary<Guid, string> chatNames = await db.Chats.AsNoTracking().Where(chat => chatIds.Contains(chat.Id)).ToDictionaryAsync(chat => chat.Id, chat => chat.Name, cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(entries.SelectMany(entry => new[] { entry.ActorId, entry.PersonId }).OfType<Guid>().ToList(), cancellationToken);
        return entries.OrderByDescending(entry => entry.At).Select(entry => new LogLine
        {
            Id = entry.Id.ToString(), At = entry.At, ActorName = ChatRows.NameOf(entry.ActorId, names), Action = entry.Action,
            Words = entry.PersonId is { } person && entry.Action is AuditActions.Paused or AuditActions.Unpaused
                ? $"{names.GetValueOrDefault(person, "Someone")}: {entry.Words}"
                : entry.Words,
            ChatId = entry.ChatId?.ToString() ?? "", ChatName = entry.ChatId is { } chat ? chatNames.GetValueOrDefault(chat, "") : "",
        }).ToList();
    }
}
