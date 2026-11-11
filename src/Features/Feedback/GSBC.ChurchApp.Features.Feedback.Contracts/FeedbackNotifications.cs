using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Feedback.Contracts;

public static class FeedbackNotifications
{
    public static readonly NotificationKind New = new(
        "feedback.new", "Someone sends feedback from the app", "Feedback", Waits: true, For: FeedbackPermissions.Triage, InAdminPanel: true, Opens: new("/feedback/{number}"));

    public static readonly IReadOnlyList<NotificationKind> All = [New];
}
