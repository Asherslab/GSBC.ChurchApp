using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class NewsHighlights(NewsDbContext db) : INewsHighlights
{
    public async Task<IReadOnlyList<HighlightedPost>> RecentAsync(int take, CancellationToken cancellationToken) =>
        (await db.Posts.AsNoTracking().Where(post => post.State == NewsState.Published).OrderByDescending(post => post.PublishedAt)
            .Take(take).ToListAsync(cancellationToken)).Select(Highlight).OfType<HighlightedPost>().ToList();

    public async Task<IReadOnlyDictionary<Guid, HighlightedPost>> PublishedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ids.Count == 0
            ? new Dictionary<Guid, HighlightedPost>()
            : (await db.Posts.AsNoTracking().Where(post => ids.Contains(post.Id) && post.State == NewsState.Published).ToListAsync(cancellationToken))
                .Select(Highlight).OfType<HighlightedPost>().ToDictionary(post => post.Id);

    private static HighlightedPost? Highlight(PostRecord post)
    {
        if (!Audience.TryParse(post.Audience, out Audience? audience))
            return null;
        RichText body = NewsRules.BodyOf(post);
        return new HighlightedPost(post.Id, post.Title, PlainText.Opening(body), NewsRules.PictureOf(body), post.PublishedAt ?? post.CreatedAt, audience,
            NewsDestinations.PostPath(post.Id.ToString()));
    }
}
