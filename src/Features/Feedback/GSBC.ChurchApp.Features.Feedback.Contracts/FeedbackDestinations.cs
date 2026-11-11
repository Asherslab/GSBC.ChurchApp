namespace GSBC.ChurchApp.Features.Feedback.Contracts;

public static class FeedbackDestinations
{
    public const string Send = "/feedback/send";
    public const string Desk = "/feedback";

    public static string Ticket(int number) => $"/feedback/{number}";
}
