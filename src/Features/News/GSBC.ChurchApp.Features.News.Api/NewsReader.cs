using System.Security.Claims;
using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class NewsReader(NewsDbContext db, PlaceAccess access)
{
    public async Task<NewsPost?> ReadAsync(ClaimsPrincipal reader, string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out Guid postId)
            || await PublishedAsync(postId, cancellationToken) is not { } post
            || !Audience.TryParse(post.Audience, out Audience? audience)
            || !await access.MayOpenAsync(reader, audience, null, cancellationToken))
            return null;
        return NewsRules.ForReaders(post);
    }

    public async Task<PostRecord?> PublishedAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Posts.AsNoTracking().SingleOrDefaultAsync(post => post.Id == id && post.State == NewsState.Published, cancellationToken);
}
