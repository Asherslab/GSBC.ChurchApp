namespace GSBC.ChurchApp.Features.Prayer.Contracts;

public static class PrayerNames
{
    public static string Short(string name)
    {
        string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return words.Length switch
        {
            0 => "",
            1 => words[0],
            _ => $"{words[0]} {char.ToUpperInvariant(words[^1][0])}.",
        };
    }
}
