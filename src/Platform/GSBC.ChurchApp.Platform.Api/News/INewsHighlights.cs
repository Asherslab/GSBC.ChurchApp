using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Api.News;

public interface INewsHighlights
{
    Task<IReadOnlyList<HighlightedPost>> RecentAsync(int take, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, HighlightedPost>> PublishedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

public sealed record HighlightedPost(Guid Id, string Title, string Opening, MediaRef? Picture, DateTime PublishedAt, Audience Audience, string Path);

public interface INewsWatcher
{
    Task PostsChangedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}

internal sealed class NoNewsHighlights : INewsHighlights
{
    public Task<IReadOnlyList<HighlightedPost>> RecentAsync(int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HighlightedPost>>([]);

    public Task<IReadOnlyDictionary<Guid, HighlightedPost>> PublishedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, HighlightedPost>>(new Dictionary<Guid, HighlightedPost>());
}
