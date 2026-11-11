using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Feedback.Contracts;

public static class FeedbackPermissions
{
    public const string SendNode = "feedback.send";
    public const string TriageNode = "feedback.triage";
    public const string PhoneDataNode = "feedback.phonedata.read";

    public const string SenderRole = "feedback-sender";
    public const string TriageRole = "feedback-triage";
    public const string PhoneDataRole = "feedback-phone-data";

    public static readonly Permission Send =
        new(SendNode, ScopeKind.Global, "On the phone: see Send feedback in More and send up to 50 a day. Anyone with a page in the admin panel can send feedback there without it", area: "Feedback");

    public static readonly Permission Triage =
        new(TriageNode, ScopeKind.Global, "Read the feedback people send, its pictures and videos, change its status, keep notes and block a phone", area: "Feedback", opensAdminArea: true);

    public static readonly Permission PhoneData =
        new(PhoneDataNode, ScopeKind.Global, "Download the copy of a phone's stored data that comes with its feedback, for finding a bug", area: "Feedback");

    public static readonly IReadOnlyList<Permission> All = [Send, Triage, PhoneData];
}
