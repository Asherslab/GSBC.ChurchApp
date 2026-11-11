namespace GSBC.ChurchApp.Features.Feedback.Contracts;

public static class FeedbackLimits
{
    public const int MostAttachments = 6;
    public const int PerPhoneADay = 5;
    public const int WithTheRoleADay = 50;
    public const int LongestText = 4000;
    public const long LargestPicture = 25L * 1024 * 1024;
    public const long LargestVideo = 150L * 1024 * 1024;
    public const long LargestPhoneData = 100L * 1024 * 1024;

    public const string NotAvailable = "Feedback isn't available on this phone.";

    public static string? TooLarge(string contentType, long bytes)
    {
        bool video = contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        long largest = video ? LargestVideo : LargestPicture;
        return bytes <= largest ? null : $"That {(video ? "video" : "picture")} is {bytes / (1024 * 1024)} MB; one can be up to {largest / (1024 * 1024)} MB.";
    }
}
