using GSBC.ChurchApp.Features.Sermons.Client;
using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Admin;

internal sealed class OnlineSermonCards(ILatestSermonsOnline latest, TimeProvider time) : ISermonCards
{
    private static readonly TimeSpan KeptFor = TimeSpan.FromMinutes(1);

    private Task<SermonCardShelf>? _loading;
    private DateTimeOffset _loadedAt;

    public IReadOnlyList<string> Feeds => [];

    public Task<SermonCardShelf> LoadAsync()
    {
        DateTimeOffset now = time.GetUtcNow();
        if (_loading is null || _loading.IsFaulted || now - _loadedAt > KeptFor)
            (_loading, _loadedAt) = (LoadNowAsync(), now);
        return _loading;
    }

    private async Task<SermonCardShelf> LoadNowAsync() =>
        new(await latest.LatestAsync(SermonsFeeds.CatalogueWindow), SermonCardShelf.Empty.Progress);
}
