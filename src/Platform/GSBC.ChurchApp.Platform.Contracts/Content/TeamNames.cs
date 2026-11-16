namespace GSBC.ChurchApp.Platform.Contracts.Content;

public static class TeamNames
{
    private const string The = "The ";
    private const string Team = "Team";

    public static string Of(string ministryName)
    {
        string name = ministryName.Trim();
        if (name.StartsWith(The, StringComparison.OrdinalIgnoreCase))
            name = name[The.Length..].TrimStart();
        if (name.Length == 0)
            return "The team";
        return name.EndsWith(Team, StringComparison.OrdinalIgnoreCase) && (name.Length == Team.Length || name[^(Team.Length + 1)] == ' ')
            ? $"{The}{name}"
            : $"{The}{name} {Team}";
    }

    public static string InSentence(string ministryName) => $"the{Of(ministryName)[3..]}";
}
