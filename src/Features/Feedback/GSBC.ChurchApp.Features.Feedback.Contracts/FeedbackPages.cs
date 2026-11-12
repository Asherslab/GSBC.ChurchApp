namespace GSBC.ChurchApp.Features.Feedback.Contracts;

public static class FeedbackPages
{
    public const int MostInATrail = 8;
    public static readonly TimeSpan ScreenshotsFrom = TimeSpan.FromHours(1);
    private static readonly string[] OnTheWayToFeedback = ["more", "feedback"];

    public static string PathOf(string baseRelative) => "/" + baseRelative.Split('?', '#')[0];

    public static string Before(IReadOnlyList<string> newestFirst, Func<string, bool> inTheMoreTab) =>
        newestFirst.FirstOrDefault(page => !IsOnTheWay(page) && !inTheMoreTab(page))
        ?? newestFirst.FirstOrDefault(page => !IsOnTheWay(page))
        ?? "";

    private static bool IsOnTheWay(string page) => OnTheWayToFeedback.Contains(page.Trim('/').Split('/')[0], StringComparer.OrdinalIgnoreCase);
}
