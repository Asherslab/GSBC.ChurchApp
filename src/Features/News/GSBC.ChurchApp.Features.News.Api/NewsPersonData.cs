using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class NewsPersonData(NewsDbContext db, NewsPublisher publisher, TimeProvider time) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        List<PostRecord> theirs = await db.Posts
            .Where(post => post.AuthorId == userId || post.AskedById == userId || post.AnsweredById == userId).ToListAsync(cancellationToken);
        foreach (PostRecord post in theirs)
        {
            if (post.AuthorId == userId)
                (post.AuthorId, post.AuthorName) = (null, "");
            if (post.AskedById == userId)
                (post.AskedById, post.AskedByName) = (null, "");
            if (post.AnsweredById == userId)
                (post.AnsweredById, post.AnsweredByName) = (null, "");
            publisher.Changed(post, now);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
