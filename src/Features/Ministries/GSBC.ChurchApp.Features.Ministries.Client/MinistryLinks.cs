using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed record MinistryLinks(Func<CalendarLine, string> Calendar, string CalendarNote, bool PostPages = false, bool WritesOnAPage = false)
{
    public static readonly MinistryLinks App = new(calendar => $"/calendar/{Uri.EscapeDataString(KeyOf(calendar))}", "See its events, or add it to your week", PostPages: true);

    public static readonly MinistryLinks Admin = new(calendar => $"/calendar?calendar={Uri.EscapeDataString(KeyOf(calendar))}", "See and change its events in Calendar", WritesOnAPage: true);

    public static string Post(string ministryId, string postId) => $"ministries/{ministryId}/news/{postId}";

    public static string Writing(string ministryId, string? postId = null, string? sunday = null) =>
        (postId is null ? $"ministries/{ministryId}/news/write" : $"ministries/{ministryId}/news/{postId}/change") + (sunday is null ? "" : $"?sunday={Uri.EscapeDataString(sunday)}");

    private static string KeyOf(CalendarLine calendar) => calendar.Slug.Length > 0 ? calendar.Slug : calendar.Id;
}
