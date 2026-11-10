using GSBC.ChurchApp.Features.Ministries.Api.Data;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal static class MinistryArchive
{
    public static IEnumerable<MinistryRecord> Active(IEnumerable<MinistryRecord> every) => every.Where(ministry => ministry.ArchivedAt is null);

    public static IReadOnlyList<MinistryRecord> Archiving(MinistryTree every, MinistryRecord ministry) =>
        [ministry, .. every.Below(ministry.Id).Where(below => below.ArchivedAt is null)];

    public static IReadOnlyList<MinistryRecord> Restoring(MinistryTree every, MinistryRecord ministry) =>
        [ministry, .. every.Below(ministry.Id).Where(below => below.ArchivedAt == ministry.ArchivedAt)];

    public static string? WhyNotRestore(MinistryTree every, MinistryRecord ministry) =>
        ministry.ArchivedAt is null ? $"{ministry.Name} isn't archived."
        : every.Above(ministry.Id).FirstOrDefault(above => above.ArchivedAt is not null) is { } archivedAbove ? $"Restore {archivedAbove.Name} first: {ministry.Name} sits under it."
        : null;
}
