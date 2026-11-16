using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed record CalendarMerge(Guid From, Guid Into);

internal sealed record CalendarFix(Guid Id, string Name, Audience Audience);

internal sealed record MinistryCalendarShape(IReadOnlyList<CalendarMerge> Merges, IReadOnlyList<CalendarFix> Fixes)
{
    public bool IsEmpty => Merges.Count == 0 && Fixes.Count == 0;
}

internal static class MinistryCalendarPlan
{
    public static string KindOf(Audience audience) => audience.Node == MinistriesPermissions.TeamViewNode ? Audiences.Team : Audiences.Everyone;

    public static Audience AudienceOf(string kind, Guid ministryId, bool hidden) => MinistryViews.CalendarAudience(kind, ministryId, hidden);

    public static MinistryCalendarShape Shape(Guid ministryId, string ministryName, bool hidden, IReadOnlyList<MinistryCalendar> calendars)
    {
        List<CalendarMerge> merges = [];
        List<CalendarFix> fixes = [];
        foreach (IGrouping<string, MinistryCalendar> kind in calendars.GroupBy(calendar => KindOf(calendar.Audience)))
        {
            Audience wanted = AudienceOf(kind.Key, ministryId, hidden);
            string name = MinistryCalendarKinds.NameOf(kind.Key, ministryName);
            MinistryCalendar keep = kind.FirstOrDefault(calendar => calendar.Audience == wanted) ?? kind.First();
            merges.AddRange(kind.Where(calendar => calendar.Id != keep.Id).Select(calendar => new CalendarMerge(calendar.Id, keep.Id)));
            if (keep.Name != name || keep.Audience != wanted)
                fixes.Add(new CalendarFix(keep.Id, name, wanted));
        }
        return new MinistryCalendarShape(merges, fixes);
    }
}
