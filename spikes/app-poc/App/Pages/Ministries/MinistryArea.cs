namespace Gsbc.AppPoc.Pages.Ministries;

public enum MinistryArea { About, News, Calendar, Files, Team, Roster, Chats }

public static class MinistryAreas
{
    public static string Slug(this MinistryArea area) => area switch
    {
        MinistryArea.About => "",
        MinistryArea.News => "news",
        MinistryArea.Calendar => "calendar",
        MinistryArea.Files => "files",
        MinistryArea.Team => "members",
        MinistryArea.Roster => "roster",
        _ => "chats",
    };

    public static MinistryArea? Parse(string? slug) =>
        Enum.GetValues<MinistryArea>().Cast<MinistryArea?>().FirstOrDefault(a => a!.Value.Slug() == (slug ?? ""));

    public static bool IsTeam(this MinistryArea area) => area is MinistryArea.Team or MinistryArea.Roster or MinistryArea.Chats;

    public static string Path(string ministryId, MinistryArea area, bool inTeamRoom = false)
    {
        var root = inTeamRoom ? $"ministries/{ministryId}/team" : $"ministries/{ministryId}";
        var slug = inTeamRoom && area is MinistryArea.News or MinistryArea.Team ? "" : area.Slug();
        return slug.Length == 0 ? root : $"{root}/{slug}";
    }
}
