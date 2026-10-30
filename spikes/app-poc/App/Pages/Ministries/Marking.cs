using Gsbc.AppPoc.Data.Ministries;

namespace Gsbc.AppPoc.Pages.Ministries;

public enum Marking { None, MembersOnly, Public }

public enum FeedScope { All, Everyone, Members }

public static class FeedScopes
{
    public static bool Includes(this FeedScope scope, Audience audience) => scope switch
    {
        FeedScope.Everyone => audience == Audience.Everyone,
        FeedScope.Members => audience == Audience.Members,
        _ => true,
    };

    public static bool Tags(this Marking marking, Audience audience) =>
        (marking == Marking.MembersOnly && audience == Audience.Members) || (marking == Marking.Public && audience == Audience.Everyone);
}
