using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MoveWords
{
    public static string Of(MovePreview preview)
    {
        List<string> lines =
        [
            $"{preview.Name} moves {(preview.To.Length > 0 ? $"under {preview.To}" : "to the top level")}{With(preview.SubMinistries)}.",
            .. preview.Gain.Select(change => $"{Leaders(change)} of {change.Ministry} will lead {preview.Name}."),
            .. preview.Lose.Select(change => $"{Leaders(change)} of {change.Ministry} won't lead {preview.Name} any more."),
        ];
        if (preview.Joins.Count > 0)
            lines.Add($"{preview.Name} joins the combined news and calendar of {MinistryWords.Names(preview.Joins)}.");
        if (preview.Leaves.Count > 0)
            lines.Add($"{preview.Name} leaves the combined news and calendar of {MinistryWords.Names(preview.Leaves)}.");
        if (preview.HiddenBy.Length > 0)
            lines.Add($"{preview.Name} will be hidden, as {preview.HiddenBy} is. Only its own people and staff will see it.");
        if (preview.ShownAgain)
            lines.Add($"{preview.Name} won't be hidden any more, so everyone will see it.");
        lines.Add("Its own people, posts, files and calendars stay as they are.");
        return string.Join(" ", lines);
    }

    private static string With(int under) => under == 0 ? "" : $", with the {MinistryWords.Ministries(under)} under it";

    private static string Leaders(LeadChange change) =>
        $"{MinistryWords.Names(change.People)}, {(change.People.Count == 1 ? "a leader" : "leaders")}";
}
