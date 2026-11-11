using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Feedback.Contracts;

public static class FeedbackNotifications
{
    public static readonly NotificationKind New = new(
        "feedback.new", "New feedback, once a day at midday when there is any", "Feedback", For: FeedbackPermissions.Triage, InAdminPanel: true,
        Opens: new(FeedbackDestinations.Desk),
        Digest: new NotificationDigest("New feedback today: 1 report", "New feedback today: {0} reports", "Open the Feedback board in the admin panel to read it.",
            DigestSchedule.Daily(new TimeOnly(12, 0))));

    public static readonly IReadOnlyList<NotificationKind> All = [New];
}
