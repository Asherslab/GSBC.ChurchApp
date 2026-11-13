namespace GSBC.ChurchApp.Platform.Permissions;

public static class RoleFeatures
{
    public const string Sermons = "Sermons";
    public const string Calendar = "Calendar";
    public const string News = "Church news";
    public const string App = "App";
    public const string Media = "Media";
    public const string People = "People";
    public const string Ministries = "Ministries";
    public const string Prayer = "Prayer";
    public const string InsightsAndHealth = "Insights and health";
    public const string Everything = "Everything";

    public static readonly IReadOnlyList<string> InOrder = [Sermons, Calendar, News, App, Media, People, Ministries, Prayer, InsightsAndHealth, Everything];
}
