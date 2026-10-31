using Gsbc.AppPoc.Data.Ministries;

namespace Gsbc.AppPoc.Pages.Ministries;

public static class ViewerRoles
{
    public static string Hint(ViewerRole role) => role switch
    {
        ViewerRole.Visitor => "Not in any ministry. Join Young Adults or Men's to see joining work.",
        ViewerRole.Member => "In Choir, Young Adults and Children's Ministry. Being in Choir makes you a Music member too.",
        ViewerRole.Leader => "Leads Music (and so its four teams) and Children's Ministry; a member of Young Adults.",
        _ => "Can open and run every ministry, and message all serving teams.",
    };

    public static string Name(ViewerRole role) => role switch
    {
        ViewerRole.Visitor => "Visitor",
        ViewerRole.Member => "Member",
        ViewerRole.Leader => "Leader",
        _ => "Staff",
    };
}
