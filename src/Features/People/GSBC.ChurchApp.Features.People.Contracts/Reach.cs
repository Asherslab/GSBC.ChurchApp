namespace GSBC.ChurchApp.Features.People.Contracts;

public static class Reach
{
    public const string Directly = "directly";
    public const string ThroughParents = "parents";
    public const string Both = "both";

    public static readonly IReadOnlyList<(string Value, string Words)> Choices =
    [
        (Directly, "Directly"),
        (ThroughParents, "Through their parents"),
        (Both, "Both"),
    ];

    public static bool Valid(string choice) => Choices.Any(known => known.Value == choice);

    public static string Effective(string? choice, bool minor, bool canSignIn, bool hasParents) =>
        !minor || !hasParents ? Directly
        : !canSignIn ? ThroughParents
        : choice is { } chosen && Valid(chosen) ? chosen
        : Both;

    public static bool ReachesThemselves(string effective) => effective is Directly or Both;

    public static bool ReachesParents(string effective) => effective is ThroughParents or Both;
}
