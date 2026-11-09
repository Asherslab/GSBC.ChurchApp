namespace GSBC.ChurchApp.Features.People.Contracts;

public static class BlueCards
{
    public const string Current = "current";
    public const string Exemption = "exemption";
    public const string Applied = "applied";
    public const string Suspended = "suspended";
    public const string Cancelled = "cancelled";
    public const string Expired = "expired";

    public static readonly IReadOnlyList<(string Value, string Words)> Statuses =
    [
        (Current, "Current"),
        (Exemption, "Exemption card"),
        (Applied, "Applied for"),
        (Suspended, "Suspended"),
        (Cancelled, "Cancelled"),
        (Expired, "Expired"),
    ];

    public const int TrainingLastsYears = 3;

    public static bool Valid(string status) => Statuses.Any(known => known.Value == status);

    public static bool Holds(string status) => status is Current or Exemption;

    public static string Words(string status) => Statuses.FirstOrDefault(known => known.Value == status).Words ?? "Not recorded";
}
