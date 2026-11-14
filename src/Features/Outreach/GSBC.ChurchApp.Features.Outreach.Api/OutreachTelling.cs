using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal sealed class OutreachTelling(INotifications notifications, OutreachMinistries ministries)
{
    public static string FollowUpSubject(Guid id) => $"outreach.follow-up/{id:N}";

    public static string ReportSubject(Guid id) => $"outreach.report/{id:N}";

    public async Task FollowUpAsync(FollowUpRecord followUp, CancellationToken cancellationToken)
    {
        foreach (string? scope in await ministries.ScopesAsync(cancellationToken))
            await notifications.RaiseAsync(new Notification(OutreachNotifications.FollowUpWanted,
                NotificationAudience.Holding(OutreachPermissions.Review, scope),
                "Someone would like a follow-up", "A walker spoke with someone who'd like the church to get in touch.",
                Link: OutreachLinks.FollowUps, Subject: FollowUpSubject(followUp.Id), ScopeId: scope) { Actor = followUp.UserId }, cancellationToken);
    }

    public async Task ReportedAsync(ReportRecord report, CancellationToken cancellationToken)
    {
        foreach (string? scope in await ministries.ScopesAsync(cancellationToken))
            await notifications.RaiseAsync(new Notification(OutreachNotifications.Reported,
                NotificationAudience.Holding(OutreachPermissions.Review, scope),
                "A place was reported", "A walker reported a place for review.",
                Link: OutreachLinks.Reports, Subject: ReportSubject(report.Id), ScopeId: scope) { Actor = report.UserId }, cancellationToken);
    }

    public Task SettleFollowUpAsync(Guid id, CancellationToken cancellationToken) => notifications.SettleAsync(FollowUpSubject(id), cancellationToken);

    public Task SettleReportAsync(Guid id, CancellationToken cancellationToken) => notifications.SettleAsync(ReportSubject(id), cancellationToken);
}
