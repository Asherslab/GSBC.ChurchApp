using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;

namespace GSBC.ChurchApp.Features.Feedback.Api;

internal enum SendVerdict
{
    Allowed,
    NotHere,
    Blocked,
    TooMany,
}

internal sealed record Sender(Guid? Install, Guid? User, bool HoldsTheRole, bool Blocked, bool UsesTheAdminPanel = false)
{
    public bool CountsByPerson => User is not null && (HoldsTheRole || UsesTheAdminPanel);
}

internal static class FeedbackRules
{
    public const int LongestText = FeedbackLimits.LongestText;
    public const int LongestNote = 2000;
    public const int LongestReason = 300;
    public const int LongestPage = 300;
    public const int LongestState = 64 * 1024;
    public const int LongestDetail = 80;
    private const int MostRecentPages = 8;
    public const int MostAttachments = FeedbackLimits.MostAttachments;
    public const int UploadAsksADay = 80;
    public const long LargestPicture = FeedbackLimits.LargestPicture;
    public const long LargestVideo = FeedbackLimits.LargestVideo;
    public const long LargestPhoneData = FeedbackLimits.LargestPhoneData;

    public static readonly TimeSpan Day = TimeSpan.FromDays(1);
    public static readonly TimeSpan KeptAfterClosing = TimeSpan.FromDays(180);
    public static readonly TimeSpan FilesKeptAfterClosing = TimeSpan.FromDays(FeedbackLimits.FilesKeptDaysAfterClosing);
    public const string SentStep = "Sent";
    public static readonly TimeSpan UnsentUploadsKeptFor = TimeSpan.FromDays(4);

    public static int DailyLimit(Sender sender) => sender.CountsByPerson ? FeedbackLimits.WithTheRoleADay : FeedbackLimits.PerPhoneADay;

    public static SendVerdict MaySend(Sender sender, int sentInTheLastDay)
    {
        if (sender is { CountsByPerson: false, Install: null })
            return SendVerdict.NotHere;
        if (sender.Blocked)
            return SendVerdict.Blocked;
        return sentInTheLastDay < DailyLimit(sender) ? SendVerdict.Allowed : SendVerdict.TooMany;
    }

    public static SendVerdict MaySendFrom(FeedbackSource source, Sender sender, int sentInTheLastDay) =>
        source == FeedbackSource.AdminPanel && sender is not { UsesTheAdminPanel: true, User: not null } ? SendVerdict.NotHere : MaySend(sender, sentInTheLastDay);

    public static string RecentPages(IEnumerable<string> pages) =>
        string.Join('\n', pages.Select(page => Clip(page, LongestPage)).Where(page => page.Length > 0).Take(MostRecentPages));

    public static bool IsClosed(TicketStatus status) => status is TicketStatus.Resolved or TicketStatus.NotDoing;

    public static bool IsDueForDeletion(TicketRecord ticket, DateTime now) =>
        IsClosed(ticket.Status) && ticket.ClosedAt is { } closed && closed <= now - KeptAfterClosing;

    public static DateTime? FilesGoAt(TicketRecord ticket) =>
        IsClosed(ticket.Status) && ticket is { ClosedAt: { } closed, AttachmentsDeleted: false } && (ticket.Attachments.Count > 0 || ticket.PhoneData is not null)
            ? closed + FilesKeptAfterClosing
            : null;

    public static bool FilesAreDue(TicketRecord ticket, DateTime now) => FilesGoAt(ticket) is { } goes && goes <= now;

    public static string Clip(string? value, int longest)
    {
        string trimmed = (value ?? "").Trim();
        return trimmed.Length <= longest ? trimmed : trimmed[..longest];
    }

    public static string FirstLine(string text)
    {
        string line = text.Split('\n', 2)[0].Trim();
        return line.Length <= 120 ? line : $"{line[..117].TrimEnd()}…";
    }

    public static string StatusName(TicketStatus status) => status switch
    {
        TicketStatus.New => "New",
        TicketStatus.InProgress => "In progress",
        TicketStatus.FixedToCheck => "Fixed, to check",
        TicketStatus.Resolved => "Resolved",
        _ => "Not doing",
    };

    public static string From(TicketRecord ticket) => ticket.SenderName.Length > 0 ? ticket.SenderName : "Not signed in";

    public static string SourceName(FeedbackSource source) => source == FeedbackSource.AdminPanel ? "the admin panel" : "the app";
}
