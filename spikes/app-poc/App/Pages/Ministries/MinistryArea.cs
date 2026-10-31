using Gsbc.AppPoc.Data.Ministries;

namespace Gsbc.AppPoc.Pages.Ministries;

public enum MinistryArea { About, News, Calendar, Roster, Chats, Files }

public static class MinistryAreas
{
    public const string PublicStream = "public";

    public static string Slug(this MinistryArea area) => area switch
    {
        MinistryArea.About => "",
        MinistryArea.News => "news",
        MinistryArea.Calendar => "calendar",
        MinistryArea.Roster => "roster",
        MinistryArea.Chats => "chats",
        _ => "files",
    };

    public static MinistryArea? Parse(string? slug) =>
        Enum.GetValues<MinistryArea>().Cast<MinistryArea?>().FirstOrDefault(a => a!.Value.Slug() == (slug ?? ""));

    public static MinistryModule? Module(this MinistryArea area) => area switch
    {
        MinistryArea.News => MinistryModule.Announcements,
        MinistryArea.Calendar => MinistryModule.Calendar,
        MinistryArea.Roster => MinistryModule.Rosters,
        MinistryArea.Chats => MinistryModule.Chats,
        MinistryArea.Files => MinistryModule.Files,
        _ => null,
    };

    public static string Label(this MinistryArea area) => area switch
    {
        MinistryArea.About => "About",
        MinistryArea.News => "News",
        MinistryArea.Calendar => "Calendar",
        MinistryArea.Roster => "Roster",
        MinistryArea.Chats => "Chats",
        _ => "Files",
    };

    public static string Path(string ministryId, MinistryArea area, Audience? stream = null)
    {
        var root = area == MinistryArea.About ? $"ministries/{ministryId}" : $"ministries/{ministryId}/{area.Slug()}";
        return stream == Audience.Everyone ? $"{root}/{PublicStream}" : root;
    }
}
