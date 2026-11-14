using Grpc.Core;
using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed record Writer(Guid Id, bool MayPublish);

internal sealed class NewsDesk(
    NewsDbContext db, NewsPublisher publisher, NewsTelling telling, IMediaReferences media, IMediaSystemFolders folders, IPeopleIndex people,
    IServiceProvider services, TimeProvider time)
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public static string MediaOwner(Guid id) => $"news/posts/{id}";

    public async Task<NewsPost> SaveAsync(Writer writer, NewsPost wanted, CancellationToken cancellationToken, IReadOnlyList<string>? uploaded = null)
    {
        if (uploaded is { Count: > 0 })
            wanted.Body = UploadedMedia.Resolve(wanted.Body, await media.VerifyAsync(uploaded.Select(id => new MediaRef { Id = id }).ToList(), cancellationToken));
        if (NewsRules.WhyNot(wanted) is { } problem)
            throw Invalid(problem);
        Guid id = ParseId(wanted.Id);
        PostRecord? post = await db.Posts.SingleOrDefaultAsync(found => found.Id == id, cancellationToken);
        if (post is null && !wanted.New)
            throw new RpcException(new Status(StatusCode.NotFound, "That post was deleted."));
        if (post is not null && wanted.New)
            return NewsRules.View(post);
        if (post is not null && NewsRules.IsShared(post))
            throw Refused($"Change it in {post.FromName}'s news: church news shows the ministry's post as it is.");
        if (post is not null && !writer.MayPublish && !NewsRules.WritersMayChange(post))
            throw Refused("It's already scheduled or published, so only someone who publishes church news can change it.");
        if (post is { State: NewsState.Archived })
            throw Refused("It's archived. Put it back first, then change it.");

        DateTime now = Now;
        if (post is null)
        {
            post = db.Posts.Add(new PostRecord
            {
                Id = id, AuthorId = writer.Id, AuthorName = await NameAsync(writer.Id, cancellationToken), CreatedAt = now, State = NewsState.Draft,
            }).Entity;
        }
        else if (post.State == NewsState.Published)
            post.EditedAt = now;

        RichText body = await VerifiedAsync(wanted.Body, cancellationToken);
        post.Title = wanted.Title.Trim();
        post.Body = ContentJson.Write(body);
        post.Audience = wanted.Audience;
        (post.LinkPath, post.LinkLabel) = (wanted.LinkPath.Trim(), wanted.LinkPath.Trim().Length == 0 ? "" : wanted.LinkLabel.Trim());
        if (post.ToldAt is null)
            (post.Tell, post.TellTitle, post.TellBody, post.Urgent) = (wanted.Tell, wanted.TellTitle.Trim(), wanted.TellBody.Trim(), wanted.Tell && wanted.Urgent);
        post.ChangedAt = now;
        publisher.Changed(post, now);
        await db.SaveChangesAsync(cancellationToken);
        await UseMediaAsync(post, body, cancellationToken);
        return NewsRules.View(post);
    }

    public async Task<NewsPost> PublishAsync(Writer writer, Guid id, DateTime? at, CancellationToken cancellationToken)
    {
        MustPublish(writer);
        PostRecord post = await FindAsync(id, cancellationToken);
        if (post.State == NewsState.Published)
            return NewsRules.View(post);
        if (post.State is NewsState.Asked or NewsState.Declined)
            throw Refused("Answer the ministry's ask instead: put it in church news, or say no.");
        if (post.State == NewsState.Archived)
            throw Refused("It's archived. Put it back first, then publish it.");
        DateTime now = Now;
        if (at is { } when && when > now.AddMinutes(1))
        {
            (post.State, post.PublishAt, post.ChangedAt) = (NewsState.Scheduled, when, now);
            publisher.Changed(post, now);
            await db.SaveChangesAsync(cancellationToken);
            await telling.SettleAsync(post.Id, cancellationToken);
            return NewsRules.View(post);
        }
        await PublishNowAsync(post, writer.Id, cancellationToken);
        return NewsRules.View(post);
    }

    public async Task PublishNowAsync(PostRecord post, Guid? actor, CancellationToken cancellationToken)
    {
        DateTime now = Now;
        (post.State, post.PublishedAt, post.PublishAt, post.ChangedAt) = (NewsState.Published, now, null, now);
        bool tell = post.Tell && post.ToldAt is null;
        if (tell)
            post.ToldAt = now;
        publisher.Changed(post, now);
        await db.SaveChangesAsync(cancellationToken);
        await telling.SettleAsync(post.Id, cancellationToken);
        if (tell)
            await telling.TellAsync(post, actor, cancellationToken);
    }

    public async Task<NewsPost> AskToPublishAsync(Writer writer, Guid id, CancellationToken cancellationToken)
    {
        PostRecord post = await FindAsync(id, cancellationToken);
        if (post.State != NewsState.Draft)
            return NewsRules.View(post);
        DateTime now = Now;
        (post.State, post.ChangedAt) = (NewsState.WaitingToPublish, now);
        publisher.Changed(post, now);
        await db.SaveChangesAsync(cancellationToken);
        await telling.WaitingAsync(post, writer.Id, cancellationToken);
        return NewsRules.View(post);
    }

    public async Task<NewsPost> BackToWritingAsync(Writer writer, Guid id, CancellationToken cancellationToken)
    {
        PostRecord post = await FindAsync(id, cancellationToken);
        if (post.State is not (NewsState.WaitingToPublish or NewsState.Scheduled))
            return NewsRules.View(post);
        if (post.State == NewsState.Scheduled)
            MustPublish(writer);
        DateTime now = Now;
        (post.State, post.PublishAt, post.ChangedAt) = (NewsState.Draft, null, now);
        publisher.Changed(post, now);
        await db.SaveChangesAsync(cancellationToken);
        await telling.SettleAsync(post.Id, cancellationToken);
        return NewsRules.View(post);
    }

    public async Task<NewsPost> ArchiveAsync(Writer writer, Guid id, CancellationToken cancellationToken)
    {
        PostRecord post = await FindAsync(id, cancellationToken);
        if (post.State == NewsState.Archived)
            return NewsRules.View(post);
        if (NewsRules.IsShared(post))
            throw Refused($"Take it off church news instead: it's {post.FromName}'s post.");
        if (!writer.MayPublish && !NewsRules.WritersMayChange(post))
            throw Refused("Only someone who publishes church news can archive it once it's scheduled or published.");
        DateTime now = Now;
        (post.State, post.PublishAt, post.ChangedAt) = (NewsState.Archived, null, now);
        publisher.Changed(post, now);
        await db.SaveChangesAsync(cancellationToken);
        await telling.SettleAsync(post.Id, cancellationToken);
        await telling.RecallAsync(post.Id, cancellationToken);
        return NewsRules.View(post);
    }

    public async Task<NewsPost> PutBackAsync(Writer writer, Guid id, CancellationToken cancellationToken)
    {
        PostRecord post = await FindAsync(id, cancellationToken);
        if (post.State != NewsState.Archived)
            return NewsRules.View(post);
        if (!writer.MayPublish && !NewsRules.WritersMayChange(post))
            throw Refused("Only someone who publishes church news can put back a post that was published.");
        DateTime now = Now;
        (post.State, post.ChangedAt) = (NewsState.Draft, now);
        publisher.Changed(post, now);
        await db.SaveChangesAsync(cancellationToken);
        return NewsRules.View(post);
    }

    public async Task DeleteAsync(Writer writer, Guid id, CancellationToken cancellationToken)
    {
        if (await db.Posts.SingleOrDefaultAsync(found => found.Id == id, cancellationToken) is not { } post)
            return;
        bool shared = NewsRules.IsShared(post);
        if (!shared && post.State != NewsState.Archived)
            throw Refused("Archive it first: only an archived post can be deleted for good.");
        if (shared ? !writer.MayPublish : !writer.MayPublish && !NewsRules.WritersMayChange(post))
            throw Refused("Only someone who publishes church news can delete it once it's been published.");
        await RemoveAsync(post, cancellationToken);
        await TellSourceAsync(post, SharedState.None, cancellationToken);
    }

    private async Task TellSourceAsync(PostRecord post, SharedState state, CancellationToken cancellationToken)
    {
        if (post is { Source: { } source, SourceId: { } sourceId }
            && services.GetServices<ISharedPostSource>().FirstOrDefault(candidate => candidate.Source == source) is { } owner)
            await owner.StateChangedAsync(sourceId, state, cancellationToken);
    }

    public async Task RemoveAsync(PostRecord post, CancellationToken cancellationToken)
    {
        db.Posts.Remove(post);
        publisher.Deleted(post.Id, Now);
        await db.SaveChangesAsync(cancellationToken);
        await telling.SettleAsync(post.Id, cancellationToken);
        await telling.RecallAsync(post.Id, cancellationToken);
        if (!NewsRules.IsShared(post))
            await media.UseAsync(MediaOwner(post.Id), [], cancellationToken);
    }

    public async Task TryOnPhoneAsync(Writer writer, Guid id, CancellationToken cancellationToken) =>
        await telling.TryOnPhoneAsync(await FindAsync(id, cancellationToken), writer.Id, cancellationToken);

    public async Task<NewsPost> AnswerAsync(Writer writer, Guid id, bool yes, bool tell, CancellationToken cancellationToken)
    {
        MustPublish(writer);
        PostRecord post = await FindAsync(id, cancellationToken);
        if (post.State is not (NewsState.Asked or NewsState.Declined))
            return NewsRules.View(post);
        (post.AnsweredById, post.AnsweredByName) = (writer.Id, await NameAsync(writer.Id, cancellationToken));
        if (yes)
        {
            post.Tell = tell;
            await PublishNowAsync(post, writer.Id, cancellationToken);
        }
        else
        {
            DateTime now = Now;
            (post.State, post.ChangedAt) = (NewsState.Declined, now);
            publisher.Changed(post, now);
            await db.SaveChangesAsync(cancellationToken);
            await telling.SettleAsync(post.Id, cancellationToken);
        }
        await TellSourceAsync(post, yes ? SharedState.Shared : SharedState.Declined, cancellationToken);
        await telling.AnsweredAsync(post, yes, writer.Id, cancellationToken);
        return NewsRules.View(post);
    }

    public async Task OfferAsync(SharedPost shared, Guid by, bool asked, bool tell, CancellationToken cancellationToken)
    {
        PostRecord? post = await SharedAsync(shared.Source, shared.SourceId, cancellationToken);
        if (post is { State: NewsState.Published })
            return;
        DateTime now = Now;
        post ??= db.Posts.Add(new PostRecord
        {
            Id = Guid.CreateVersion7(), Source = shared.Source, SourceId = shared.SourceId, Audience = Audience.Everyone.Text, CreatedAt = now,
        }).Entity;
        Copy(shared, post);
        if (asked)
        {
            (post.State, post.AskedById, post.AskedByName, post.ChangedAt) = (NewsState.Asked, by, await NameAsync(by, cancellationToken), now);
            publisher.Changed(post, now);
            await db.SaveChangesAsync(cancellationToken);
            await telling.AskedAsync(post, by, cancellationToken);
            return;
        }
        (post.AnsweredById, post.AnsweredByName, post.Tell) = (by, await NameAsync(by, cancellationToken), tell);
        await PublishNowAsync(post, by, cancellationToken);
    }

    public async Task SourceChangedAsync(SharedPost shared, CancellationToken cancellationToken)
    {
        if (await SharedAsync(shared.Source, shared.SourceId, cancellationToken) is not { } post)
            return;
        DateTime now = Now;
        Copy(shared, post);
        post.ChangedAt = now;
        publisher.Changed(post, now);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task WithdrawAsync(string source, Guid sourceId, CancellationToken cancellationToken)
    {
        if (await SharedAsync(source, sourceId, cancellationToken) is { } post)
            await RemoveAsync(post, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, SharedState>> StatesAsync(string source, IReadOnlyCollection<Guid> sourceIds, CancellationToken cancellationToken) =>
        (await db.Posts.AsNoTracking().Where(post => post.Source == source && post.SourceId != null && sourceIds.Contains(post.SourceId.Value))
            .Select(post => new { post.SourceId, post.State }).ToListAsync(cancellationToken))
        .ToDictionary(post => post.SourceId!.Value, post => post.State switch
        {
            NewsState.Published => SharedState.Shared,
            NewsState.Declined => SharedState.Declined,
            _ => SharedState.Asked,
        });

    public async Task<IReadOnlyList<NewsPost>> ListAsync(CancellationToken cancellationToken) =>
        (await db.Posts.AsNoTracking().OrderByDescending(post => post.PublishedAt ?? post.ChangedAt).ToListAsync(cancellationToken)).Select(NewsRules.View).ToList();

    public async Task PublishDueAsync(CancellationToken cancellationToken)
    {
        DateTime now = Now;
        List<Guid> due = await db.Posts.Where(post => post.State == NewsState.Scheduled && post.PublishAt <= now).Select(post => post.Id).ToListAsync(cancellationToken);
        foreach (Guid id in due)
        {
            int claimed = await db.Posts.Where(post => post.Id == id && post.State == NewsState.Scheduled)
                .ExecuteUpdateAsync(set => set.SetProperty(post => post.State, NewsState.Published), cancellationToken);
            if (claimed == 0)
                continue;
            PostRecord post = await db.Posts.SingleAsync(found => found.Id == id, cancellationToken);
            await PublishNowAsync(post, post.AuthorId, cancellationToken);
        }
    }

    private static void Copy(SharedPost shared, PostRecord post)
    {
        post.Title = shared.Title;
        post.Body = ContentJson.Write(shared.Body);
        (post.FromId, post.FromName, post.FromLink) = (shared.FromId, shared.FromName, shared.Link);
        (post.AuthorName, post.EditedAt) = (shared.AuthorName, shared.EditedAt);
        if (post.CreatedAt == default || post.CreatedAt > shared.PostedAt)
            post.CreatedAt = shared.PostedAt;
    }

    private Task<PostRecord?> SharedAsync(string source, Guid sourceId, CancellationToken cancellationToken) =>
        db.Posts.SingleOrDefaultAsync(post => post.Source == source && post.SourceId == sourceId, cancellationToken);

    private async Task<RichText> VerifiedAsync(RichText body, CancellationToken cancellationToken)
    {
        RichText used = RichTextRules.WithOnlyUsedMedia(body);
        if (used.Media.Count == 0)
            return new RichText { Markdown = body.Markdown.Trim() };
        IReadOnlyList<MediaRef> verified = await media.VerifyAsync(used.Media, cancellationToken);
        return new RichText { Markdown = body.Markdown.Trim(), Media = [.. verified] };
    }

    private async Task UseMediaAsync(PostRecord post, RichText body, CancellationToken cancellationToken)
    {
        await media.UseAsync(MediaOwner(post.Id), body.Media, cancellationToken);
        if (body.Media.Count > 0)
            await folders.FileAsync(NewsMedia.Area, null, "Church news", body.Media, cancellationToken);
    }

    private async Task<PostRecord> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Posts.SingleOrDefaultAsync(post => post.Id == id, cancellationToken)
        ?? throw new RpcException(new Status(StatusCode.NotFound, "That post was deleted."));

    private async Task<string> NameAsync(Guid person, CancellationToken cancellationToken) =>
        (await people.NamesAsync([person], cancellationToken)).GetValueOrDefault(person) ?? "";

    private static void MustPublish(Writer writer)
    {
        if (!writer.MayPublish)
            throw Refused("Only someone who publishes church news can do that. Ask for it to be published instead.");
    }

    public static Guid ParseId(string id) =>
        Guid.TryParse(id, out Guid parsed) ? parsed : throw Invalid("That isn't a post.");

    private static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static RpcException Refused(string message) => new(new Status(StatusCode.PermissionDenied, message));
}
