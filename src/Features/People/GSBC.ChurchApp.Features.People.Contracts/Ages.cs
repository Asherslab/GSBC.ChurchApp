namespace GSBC.ChurchApp.Features.People.Contracts;

public static class Ages
{
    public const int Adult = 18;

    public static int YearsOn(DateOnly born, DateOnly day)
    {
        int years = day.Year - born.Year;
        return day < born.AddYears(years) ? years - 1 : years;
    }

    public static bool IsMinor(DateOnly? born, DateOnly today) => born is { } day && YearsOn(day, today) < Adult;

    public static DateOnly ComesOfAge(DateOnly born) => born.AddYears(Adult);
}
