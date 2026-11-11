using GSBC.ChurchApp.Features.Feedback.Contracts;

namespace GSBC.ChurchApp.Features.Feedback.Admin;

internal static class FeedbackWords
{
    public static readonly IReadOnlyList<TicketStatus> Board = [TicketStatus.New, TicketStatus.InProgress, TicketStatus.FixedToCheck, TicketStatus.Resolved];

    public static string Status(TicketStatus status) => status switch
    {
        TicketStatus.New => "New",
        TicketStatus.InProgress => "In progress",
        TicketStatus.FixedToCheck => "Fixed, to check",
        TicketStatus.Resolved => "Resolved",
        _ => "Not doing",
    };

    public static string Step(TicketStatus status) => status == TicketStatus.FixedToCheck ? "Fixed" : Status(status);

    public static bool SaysMore(string text) => text.Trim().Length > 0 && Title(text) != text.Trim();

    public static string Title(string text)
    {
        string line = text.Split('\n', 2)[0].Trim();
        if (line.Length == 0)
            return "Screenshots, no words";
        return line.Length <= 90 ? line : $"{line[..87].TrimEnd()}…";
    }

    public static bool IsClosed(TicketStatus status) => status is TicketStatus.Resolved or TicketStatus.NotDoing;

    public static string Platform(PhoneDetails? phone) => phone?.Platform switch
    {
        "ios" => "iOS",
        "android" => "Android",
        "web" => "A browser",
        _ => "",
    };

    public static string Size(long bytes) => bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024):0.#} MB" : $"{Math.Max(1, bytes / 1024)} KB";
}
