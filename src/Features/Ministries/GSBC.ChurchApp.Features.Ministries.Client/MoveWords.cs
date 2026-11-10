using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MoveWords
{
    public static string Of(MovePreview preview)
    {
        List<string> lines =
        [
            $"{preview.Name} moves {(preview.To.Length > 0 ? $"under {preview.To}" : "to the top level")}{With(preview.SubMinistries)}.",
            .. preview.Gain.Select(change => $"{Leaders(change)} of {change.Ministry} will lead it."),
            .. preview.Lose.Select(change => $"{Leaders(change)} of {change.Ministry} won't lead it any more."),
        ];
        if (preview.Joins.Count > 0)
            lines.Add($"It joins {MinistryWords.Names(preview.Joins)}.");
        if (preview.Leaves.Count > 0)
            lines.Add($"It leaves {MinistryWords.Names(preview.Leaves)}.");
        lines.Add("Its own people, posts, files and calendars stay as they are.");
        return string.Join(" ", lines);
    }

    private static string With(int under) => under == 0 ? "" : $", with the {MinistryWords.Ministries(under)} under it";

    private static string Leaders(LeadChange change) =>
        $"{MinistryWords.Names(change.People)}, {(change.People.Count == 1 ? "a leader" : "leaders")}";
}
