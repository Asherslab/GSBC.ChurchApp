namespace GSBC.ChurchApp.Platform.Common;

public static class ChurchTime
{
    public const string ZoneId = "Australia/Brisbane";

    public static TimeZoneInfo Zone { get; } = TimeZoneInfo.FindSystemTimeZoneById(ZoneId);

    public static DateTime ToChurch(DateTime utc) =>
        utc.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(utc, Zone)
            : throw new ArgumentException("Only UTC crosses the wire.", nameof(utc));

    public static DateTime ToUtc(DateTime churchLocal) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(churchLocal, DateTimeKind.Unspecified), Zone);
}
