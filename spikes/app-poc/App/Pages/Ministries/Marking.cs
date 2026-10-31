using Gsbc.AppPoc.Data.Ministries;

namespace Gsbc.AppPoc.Pages.Ministries;

public enum Marking { None, MembersOnly }

public static class Markings
{
    public static bool Tags(this Marking marking, Audience audience) => marking == Marking.MembersOnly && audience == Audience.Members;
}
