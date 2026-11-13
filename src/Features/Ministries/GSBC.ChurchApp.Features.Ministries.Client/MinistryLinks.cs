using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed record MinistryLinks(Func<CalendarLine, string> Calendar, string CalendarNote)
{
    public static readonly MinistryLinks App = new(calendar => $"/calendar/{Uri.EscapeDataString(KeyOf(calendar))}", "See its events, or add it to your week");

    public static readonly MinistryLinks Admin = new(calendar => $"/calendar?calendar={Uri.EscapeDataString(KeyOf(calendar))}", "See and change its events in Calendar");

    public static string Post(string ministryId, string postId) => $"ministries/{ministryId}/news/{postId}";

    private static string KeyOf(CalendarLine calendar) => calendar.Slug.Length > 0 ? calendar.Slug : calendar.Id;
}
