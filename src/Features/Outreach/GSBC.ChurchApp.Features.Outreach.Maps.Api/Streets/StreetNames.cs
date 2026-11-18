using System.Text;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Streets;

internal static class StreetNames
{
    private static readonly Dictionary<string, string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Street"] = "St", ["Road"] = "Rd", ["Court"] = "Ct", ["Drive"] = "Dr", ["Crescent"] = "Cres", ["Place"] = "Pl",
        ["Avenue"] = "Ave", ["Close"] = "Cl", ["Parade"] = "Pde", ["Terrace"] = "Tce", ["Circuit"] = "Cct", ["Boulevard"] = "Bvd",
        ["Lane"] = "Ln", ["Grove"] = "Gr", ["Highway"] = "Hwy", ["Esplanade"] = "Esp", ["Square"] = "Sq", ["Circle"] = "Cir",
    };

    public static string Short(string name)
    {
        string[] words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = words.Length - 1; i >= Math.Max(0, words.Length - 2); i--)
            if (Abbreviations.TryGetValue(words[i], out string? abbreviation))
            {
                words[i] = abbreviation;
                break;
            }
        return string.Join(' ', words);
    }

    public static string Key(string name)
    {
        StringBuilder key = new(name.Length);
        foreach (string word in Short(name).Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (key.Length > 0)
                key.Append(' ');
            foreach (char letter in word)
                if (char.IsLetterOrDigit(letter))
                    key.Append(char.ToLowerInvariant(letter));
        }
        return key.ToString();
    }
}
