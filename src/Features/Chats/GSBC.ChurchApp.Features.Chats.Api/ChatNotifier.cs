using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatNotifier(ChatsDbContext db, INotifications notifications, IPeopleIndex people, TimeProvider time)
{
    public static string Link(Guid chat) => ChatsDestinations.Chat(chat.ToString());

    public static string ConsentSubject(Guid chat, Guid child) => $"chats/{chat:N}/consent/{child:N}";

    public static string ReportSubject(Guid report) => $"chats/reports/{report:N}";

    public async Task MessageAsync(Published published, MessageRecord message, CancellationToken cancellationToken)
    {
        if (message.Kind != MessageKinds.Text)
            return;

        HashSet<Guid> muted = (await db.Participants.AsNoTracking()
                .Where(participant => participant.ChatId == message.ChatId && participant.Muted)
                .Select(participant => participant.PersonId)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(
            new[] { message.SenderId, message.ForPersonId }.OfType<Guid>().ToList(), cancellationToken);
        string sender = ChatRows.NameOf(message.SenderId, names);
        if (message.ForPersonId is { } child)
            sender = $"{sender}, for {names.GetValueOrDefault(child, "their child")}";
        string title = $"{published.Chat.Name} · {published.Place?.Name}".TrimEnd(' ', '·');
        string body = $"{sender}: {ChatRows.Snippet(message)}";

        foreach (Participant recipient in published.Plan.Recipients.Where(participant => participant.PersonId != message.SenderId))
        {
            bool mentioned = message.Mentions.Contains(recipient.PersonId);
            if (muted.Contains(recipient.PersonId) && !mentioned)
                continue;
            await notifications.RaiseAsync(new Notification(
                mentioned ? ChatsNotifications.Mention : ChatsNotifications.Message, NotificationAudience.Person(recipient.PersonId), title, body,
                Link(message.ChatId), Subject: $"chats/{message.ChatId:N}/{message.Id:N}", ScopeId: published.Chat.MinistryId.ToString()) { Actor = message.SenderId }, cancellationToken);
        }
    }

    public async Task ConsentAsync(Published published, Guid actor, CancellationToken cancellationToken)
    {
        List<Participant> waiting = published.Plan.Participants.Where(participant => participant.State == ChatStates.AwaitingConsent).ToList();
        if (waiting.Count == 0 || published.Chat.LastSeq == 0)
            return;

        List<ConsentRecord> consents = await db.Consents.Where(consent => consent.ChatId == published.Chat.Id).ToListAsync(cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(waiting.Select(participant => participant.PersonId).ToList(), cancellationToken);
        foreach (Participant child in waiting)
        {
            ConsentRecord? consent = consents.FirstOrDefault(found => found.ChildId == child.PersonId);
            if (consent?.AskedAt is not null)
                continue;
            if (consent is null)
                db.Consents.Add(consent = new ConsentRecord { ChatId = published.Chat.Id, ChildId = child.PersonId });
            consent.AskedAt = time.GetUtcNow().UtcDateTime;
            string name = names.GetValueOrDefault(child.PersonId, "your child");
            foreach (Participant parent in published.Plan.Participants.Where(participant => participant.ForChildren.Contains(child.PersonId)))
            {
                await notifications.RaiseAsync(new Notification(
                    ChatsNotifications.Consent, NotificationAudience.Person(parent.PersonId), $"Is {name} OK to join {published.Chat.Name}?",
                    $"A leader in {published.Place?.Name} started a chat for {name}. Read the first message, then say yes or not now.",
                    Link(published.Chat.Id), Subject: ConsentSubject(published.Chat.Id, child.PersonId), Action: "Read and decide") { Actor = actor }, cancellationToken);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => notifications.SettleAsync(subject, cancellationToken);

    public const string ReportsPage = "/chats/reports";

    public Task ReportAsync(ReportRecord report, ChatRecord chat, string ministryName, CancellationToken cancellationToken) =>
        report.To == ReportRoutes.Auditors
            ? notifications.RaiseAsync(new Notification(
                ChatsNotifications.Urgent, NotificationAudience.Holding(ChatsPermissions.Audit), $"Urgent: a message was reported in {chat.Name}",
                $"In {ministryName}. Read it in context in the admin panel, under Chats.", ReportsPage, Subject: ReportSubject(report.Id), Urgent: true) { Actor = report.ReporterId },
                cancellationToken)
            : notifications.RaiseAsync(new Notification(
                ChatsNotifications.Report, NotificationAudience.Holding(ChatsPermissions.Check, chat.MinistryId.ToString()), $"A message was reported in {chat.Name}",
                $"In {ministryName}. Open the chat to read it, or check it in the admin panel.", Link(chat.Id), Subject: ReportSubject(report.Id),
                Action: "Open the chat") { Actor = report.ReporterId }, cancellationToken);
}
