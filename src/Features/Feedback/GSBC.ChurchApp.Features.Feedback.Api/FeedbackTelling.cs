using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal sealed class FeedbackTelling(INotifications notifications)
{
    private static string Subject(Guid ticket) => $"feedback/{ticket:N}";

    public Task NewAsync(TicketRecord ticket, CancellationToken cancellationToken) =>
        notifications.RaiseAsync(new Notification(FeedbackNotifications.New, NotificationAudience.Holding(FeedbackPermissions.Triage),
            $"New feedback #{ticket.Number} from {FeedbackRules.SourceName(ticket.Source)}", $"{FeedbackRules.From(ticket)}: {FeedbackRules.FirstLine(ticket.Text)}",
            FeedbackDestinations.Ticket(ticket.Number), Subject(ticket.Id), Action: "Look at it") { Actor = ticket.UserId }, cancellationToken);

    public Task SettledAsync(TicketRecord ticket, CancellationToken cancellationToken) => notifications.SettleAsync(Subject(ticket.Id), cancellationToken);
}

internal static class FeedbackMarks
{
    public static void Changed(FeedbackDbContext db, Guid ticket, DateTime now, bool deleted = false) =>
        db.FeedChanges.Add(FeedChangeRecord.Marker(FeedbackFeeds.Desk, ticket.ToString(), now, deleted));

    public static void PhoneChanged(FeedbackDbContext db, Guid install, DateTime now) =>
        db.FeedChanges.Add(FeedChangeRecord.Marker(FeedbackFeeds.Desk, $"phone/{install}", now));
}
