namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class ListeningProgress
{
    public const double FinishingSeconds = 30;
    public const double FinishingFraction = 0.97;
    public const double StartedSeconds = 30;

    public static ListeningEntry Merge(ListeningEntry first, ListeningEntry second)
    {
        ListeningEntry heard = Later(first, second);
        return new ListeningEntry
        {
            SermonId = first.SermonId.Length > 0 ? first.SermonId : second.SermonId,
            Position = heard.Position,
            HeardAt = heard.HeardAt,
            Duration = Math.Max(first.Duration, second.Duration),
            FinishedAt = Latest(first.FinishedAt, second.FinishedAt),
            WatchedAt = Latest(first.WatchedAt, second.WatchedAt),
            ClearedAt = Latest(first.ClearedAt, second.ClearedAt),
        };
    }

    public static ListeningEntry? MergeEither(ListeningEntry? first, ListeningEntry? second) =>
        (first, second) switch
        {
            (null, _) => second,
            (_, null) => first,
            _ => Merge(first, second),
        };

    public static bool IsFinished(ListeningEntry entry) => After(entry.FinishedAt, entry.ClearedAt);

    public static bool IsWatched(ListeningEntry entry) => After(entry.WatchedAt, entry.ClearedAt);

    public static bool IsUnderway(ListeningEntry entry) =>
        !IsFinished(entry) && entry.Position >= StartedSeconds && After(entry.HeardAt, entry.ClearedAt);

    public static bool ReachesTheEnd(double position, double duration) =>
        duration > 0 && position >= duration - Math.Min(FinishingSeconds, duration * (1 - FinishingFraction));

    public static double Fraction(ListeningEntry entry) =>
        IsFinished(entry) ? 1 : entry.Duration > 0 ? Math.Clamp(entry.Position / entry.Duration, 0, 1) : 0;

    public static ListeningEntry Heard(string sermonId, double position, double duration, DateTime at) => new()
    {
        SermonId = sermonId,
        Position = position,
        Duration = duration,
        HeardAt = at,
        FinishedAt = ReachesTheEnd(position, duration) ? at : null,
    };

    public static ListeningEntry Played(string sermonId, DateTime at) => new() { SermonId = sermonId, FinishedAt = at };

    public static ListeningEntry Watched(string sermonId, DateTime at) => new() { SermonId = sermonId, WatchedAt = at };

    public static ListeningEntry NotPlayed(string sermonId, DateTime at) => new() { SermonId = sermonId, HeardAt = at, ClearedAt = at };

    private static ListeningEntry Later(ListeningEntry first, ListeningEntry second)
    {
        int byTime = Nullable.Compare(first.HeardAt, second.HeardAt);
        return byTime > 0 || (byTime == 0 && first.Position >= second.Position) ? first : second;
    }

    private static DateTime? Latest(DateTime? first, DateTime? second) => Nullable.Compare(first, second) >= 0 ? first : second;

    private static bool After(DateTime? mark, DateTime? clearedAt) => mark is { } at && (clearedAt is not { } cleared || at > cleared);
}
