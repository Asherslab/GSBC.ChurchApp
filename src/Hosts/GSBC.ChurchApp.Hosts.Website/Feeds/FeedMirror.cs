using GSBC.ChurchApp.Platform.Contracts.Sync;

namespace GSBC.ChurchApp.Hosts.Website.Feeds;

public sealed class FeedMirror(IEnumerable<string> feeds)
{
    private readonly Dictionary<string, Dictionary<string, FeedChange>> _rows = feeds.ToDictionary(feed => feed, _ => new Dictionary<string, FeedChange>());
    private readonly Dictionary<string, long> _cursors = [];

    public string? Origin { get; private set; }

    public List<FeedCursor> Cursors => _rows.Keys.Select(feed => new FeedCursor { Feed = feed, Cursor = _cursors.GetValueOrDefault(feed) }).ToList();

    public IReadOnlyCollection<FeedChange> Rows(string feed) => _rows[feed].Values;

    public bool Apply(string? origin, IEnumerable<FeedBatch> batches)
    {
        if (!string.IsNullOrEmpty(origin))
            Origin = origin;

        bool changed = false;
        foreach (FeedBatch batch in batches)
        {
            if (!_rows.TryGetValue(batch.Feed, out Dictionary<string, FeedChange>? rows))
                continue;

            if (batch.Reset)
            {
                changed |= rows.Count > 0;
                rows.Clear();
            }

            foreach (FeedChange change in batch.Changes)
            {
                changed = true;
                if (change.Deleted)
                    rows.Remove(change.Id);
                else
                    rows[change.Id] = change;
            }

            _cursors[batch.Feed] = batch.Cursor;
        }

        return changed;
    }
}
