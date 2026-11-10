namespace GSBC.ChurchApp.Platform.Permissions;

public static class RoleFeatures
{
    public const string Sermons = "Sermons";
    public const string Calendar = "Calendar";
    public const string Announcements = "Announcements";
    public const string App = "App";
    public const string Media = "Media";
    public const string People = "People";
    public const string Ministries = "Ministries";
    public const string InsightsAndHealth = "Insights and health";
    public const string Everything = "Everything";

    public static readonly IReadOnlyList<string> InOrder = [Sermons, Calendar, Announcements, App, Media, People, Ministries, InsightsAndHealth, Everything];
}
