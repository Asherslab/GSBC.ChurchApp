using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Contracts.News;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryChurchNews(MinistriesDbContext db, MinistryViews views, MinistryChanges changes, IChurchNews news) : ISharedPostSource
{
    public string Source => ChurchNews.FromMinistries;

    public static string? WhyNotChurchWide(PostRecord post, MinistryRecord? ministry, MinistryTree tree) =>
        ministry is null || tree.Find(ministry.Id) is null ? "This ministry is archived, so its posts stay in it."
        : post.Audience != Audiences.Everyone ? "Only a post for everyone can go in church news."
        : tree.IsHidden(ministry.Id) ? "A hidden ministry's posts stay in the ministry."
        : !MinistryParts.IsOn(ministry.PartsOff, MinistryParts.News) ? "This ministry has News turned off."
        : null;

    public async Task AfterSaveAsync(PostRecord post, MinistryRecord ministry, MinistryTree tree, bool ask, Guid me, CancellationToken cancellationToken)
    {
        if (WhyNotChurchWide(post, ministry, tree) is not null)
        {
            await WithdrawAsync(post, cancellationToken);
            return;
        }
        if (post.ChurchNews is SharedState.Shared or SharedState.Asked)
            await news.ChangedAsync(Shared(post, ministry), cancellationToken);
        else if (ask)
        {
            await news.AskAsync(Shared(post, ministry), me, cancellationToken);
            post.ChurchNews = SharedState.Asked;
        }
    }

    public async Task ShareAsync(PostRecord post, MinistryRecord ministry, MinistryTree tree, Guid me, bool tell, CancellationToken cancellationToken)
    {
        if (WhyNotChurchWide(post, ministry, tree) is { } problem)
            throw MinistriesService.Refused(problem);
        if (post.ChurchNews == SharedState.Shared)
            return;
        await news.ShareAsync(Shared(post, ministry), me, tell, cancellationToken);
        post.ChurchNews = SharedState.Shared;
    }

    public async Task WithdrawAsync(PostRecord post, CancellationToken cancellationToken)
    {
        if (post.ChurchNews == SharedState.None)
            return;
        await news.WithdrawAsync(Source, post.Id, cancellationToken);
        post.ChurchNews = SharedState.None;
    }

    public async Task StateChangedAsync(Guid sourceId, SharedState state, CancellationToken cancellationToken)
    {
        if (await db.Posts.SingleOrDefaultAsync(candidate => candidate.Id == sourceId, cancellationToken) is not { } post || post.ChurchNews == state)
            return;
        post.ChurchNews = state;
        changes.Touch(post.MinistryId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task KeepUpAsync(CancellationToken cancellationToken)
    {
        List<PostRecord> waiting = await db.Posts
            .Where(post => post.ChurchNews != SharedState.None || (post.WholeChurchAskedAt != null && post.ChurchNews == SharedState.None))
            .ToListAsync(cancellationToken);
        if (waiting.Count == 0)
            return;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        Dictionary<Guid, MinistryRecord> ministries = await db.Ministries.AsNoTracking()
            .Where(ministry => waiting.Select(post => post.MinistryId).Contains(ministry.Id)).ToDictionaryAsync(ministry => ministry.Id, cancellationToken);
        foreach (PostRecord post in waiting)
        {
            MinistryRecord? ministry = ministries.GetValueOrDefault(post.MinistryId);
            bool eligible = WhyNotChurchWide(post, ministry, tree) is null;
            if (post.WholeChurchAskedAt is not null && post.ChurchNews == SharedState.None)
            {
                post.WholeChurchAskedAt = null;
                if (eligible)
                {
                    await news.AskAsync(Shared(post, ministry!), post.AuthorId, cancellationToken);
                    post.ChurchNews = SharedState.Asked;
                }
            }
            else if (!eligible)
                await WithdrawAsync(post, cancellationToken);
            else
                continue;
            changes.Touch(post.MinistryId);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private SharedPost Shared(PostRecord post, MinistryRecord ministry) => new(
        Source, post.Id, ministry.Id, ministry.Name, post.Title, MinistryViews.ContentOf(post), post.AuthorName, post.PostedAt, post.EditedAt,
        $"/ministries/{ministry.Id}/news");
}
