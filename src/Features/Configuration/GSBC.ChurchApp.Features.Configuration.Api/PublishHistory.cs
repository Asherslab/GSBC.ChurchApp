using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed record Rollback(Guid BatchId, Guid RollsBack, DateTime At, Guid By);

internal static class PublishHistory
{
    public static void MarkUndone(
        IEnumerable<PublishedDraft> published, IReadOnlyCollection<ChangeRecord> rows, IReadOnlyCollection<Rollback> undos,
        IReadOnlyCollection<Rollback> redos, IReadOnlyDictionary<Guid, PlacementState> live)
    {
        foreach (PublishedDraft entry in published)
        {
            Guid batchId = Guid.Parse(entry.BatchId);
            Rollback? latest = undos.Where(undo => undo.RollsBack == batchId).MaxBy(undo => undo.At);
            if (latest is null || redos.Any(redo => redo.RollsBack == latest.BatchId && redo.At >= latest.At)
                || StillAsPublished(rows.Where(row => row.BatchId == batchId), live))
                continue;
            entry.UndoneBatchId = latest.BatchId.ToString();
            entry.UndoneAt = latest.At;
            entry.UndoneBy = latest.By.ToString();
        }
    }

    public static string NothingToChange(IReadOnlyList<string> areaTitles, string? draftName, string? undoneDraftName)
    {
        string places = "the " + string.Join(" and ", areaTitles.Select((title, index) => index == 0 ? title : title.ToLowerInvariant()));
        (string looks, string did) = areaTitles.Count == 1 ? ("looks", "it did") : ("look", "they did");
        if (draftName is not null)
            return $"Nothing to change: {places} already {looks} as {did} before “{draftName}” was published.";
        return undoneDraftName is null
            ? $"Nothing to change: {places} already {looks} like that."
            : $"Nothing to change: {places} already {looks} as “{undoneDraftName}” left {(areaTitles.Count == 1 ? "it" : "them")}.";
    }

    private static bool StillAsPublished(IEnumerable<ChangeRecord> batch, IReadOnlyDictionary<Guid, PlacementState> live) =>
        batch.All(change => PlacementState.Same(live.GetValueOrDefault(change.PlacementId), PlacementState.FromJson(change.After)));
}
