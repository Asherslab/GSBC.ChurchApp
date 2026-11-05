using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed record Underway(Sermon Sermon, ListeningEntry Progress);

public static class KeepGoing
{
    public static readonly TimeSpan Within = TimeSpan.FromDays(7);

    public static Underway? Pick(SermonShelf shelf, IReadOnlyDictionary<string, ListeningEntry> progress, DateTime nowUtc) =>
        progress.Values
            .Where(entry => ListeningProgress.IsUnderway(entry) && entry.HeardAt is { } heard && nowUtc - heard <= Within)
            .OrderByDescending(entry => entry.HeardAt)
            .Select(entry => shelf.Find(entry.SermonId) is { } sermon ? new Underway(sermon, entry) : null)
            .FirstOrDefault(underway => underway is not null);

    public static IReadOnlyList<Underway> All(SermonShelf shelf, IReadOnlyDictionary<string, ListeningEntry> progress) =>
        progress.Values
            .Where(ListeningProgress.IsUnderway)
            .OrderByDescending(entry => entry.HeardAt)
            .Select(entry => shelf.Find(entry.SermonId) is { } sermon ? new Underway(sermon, entry) : null)
            .OfType<Underway>()
            .ToList();
}
