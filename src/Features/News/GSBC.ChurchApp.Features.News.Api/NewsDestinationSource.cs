using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class NewsDestinationSource(NewsReader reader) : IDestinationSource
{
    public DestinationKind Kind => NewsDestinations.Post;

    public Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FoundPlace>>([]);

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken) =>
        Guid.TryParse(id, out Guid postId) && await reader.PublishedAsync(postId, cancellationToken) is { } post
            ? new FoundPlace(post.Id.ToString(), post.Title, "Church news", Audience.TryParse(post.Audience, out Audience? audience) ? audience : Audience.SignedIn, When: post.PublishedAt)
            : null;

    public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
