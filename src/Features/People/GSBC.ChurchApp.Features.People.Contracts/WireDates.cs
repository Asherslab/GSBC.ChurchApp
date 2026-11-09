namespace GSBC.ChurchApp.Features.People.Contracts;

public static class WireDates
{
    public static DateTime ToWire(DateOnly day) => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public static DateTime? ToWire(DateOnly? day) => day is { } value ? ToWire(value) : null;

    public static DateOnly FromWire(DateTime value) => DateOnly.FromDateTime(value);

    public static DateOnly? FromWire(DateTime? value) => value is { } day ? FromWire(day) : null;
}
