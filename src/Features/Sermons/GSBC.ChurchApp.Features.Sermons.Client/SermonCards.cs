using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Client.Content;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed record SermonCardShelf(IReadOnlyList<Sermon> Sermons, IReadOnlyDictionary<string, ListeningEntry> Progress)
{
    public static readonly SermonCardShelf Empty = new([], new Dictionary<string, ListeningEntry>());

    private const int MostChoices = 40;

    public Sermon? Find(string id) => Sermons.FirstOrDefault(sermon => sermon.Id == id);

    public ListeningEntry? ProgressOf(Sermon sermon) => Progress.GetValueOrDefault(sermon.Id);

    public IReadOnlyList<AppCardChoice> Choices(string text) =>
        Sermons
            .Where(sermon => text.Length == 0 || sermon.Title.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                || sermon.SpeakerName.Contains(text, StringComparison.CurrentCultureIgnoreCase) || (sermon.SeriesName ?? "").Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(sermon => sermon.PreachedAt)
            .Take(MostChoices)
            .Select(sermon => new AppCardChoice(new AppCard(AppCardKind.Sermon, sermon.Id), sermon.Title,
                SermonText.Join(SermonText.Day(sermon.PreachedAt), sermon.SpeakerName, sermon.SeriesName), sermon.Artwork))
            .ToList();
}

public interface ISermonCards
{
    IReadOnlyList<string> Feeds { get; }

    Task<SermonCardShelf> LoadAsync();
}

internal sealed class StoredSermonCards(SermonCatalogue catalogue, Listening listening) : ISermonCards
{
    public IReadOnlyList<string> Feeds => [.. SermonCatalogue.Feeds, .. Listening.Feeds];

    public async Task<SermonCardShelf> LoadAsync() => new((await catalogue.LoadAsync()).Sermons, await listening.LoadAsync());
}

internal sealed class SermonCardChoices(ISermonCards cards) : IAppCardSource
{
    public IReadOnlyList<AppCardKind> Kinds => [AppCardKind.Sermon];

    public IReadOnlyList<string> Feeds => cards.Feeds;

    public async Task<IReadOnlyList<AppCardChoice>> ChoicesAsync(AppCardKind kind, string text) => (await cards.LoadAsync()).Choices(text.Trim());
}
