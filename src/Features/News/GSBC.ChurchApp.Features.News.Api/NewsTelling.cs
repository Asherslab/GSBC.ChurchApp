using GSBC.ChurchApp.Features.News.Api.Data;
using GSBC.ChurchApp.Features.News.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class NewsTelling(INotifications notifications)
{
    public static string Subject(Guid id) => $"news/posts/{id:N}";

    private static string WaitingSubject(Guid id) => $"news/waiting/{id:N}";

    private static string AskSubject(Guid id) => $"news/asked/{id:N}";

    public async Task TellAsync(PostRecord post, Guid? actor, CancellationToken cancellationToken)
    {
        List<Notification> ways = AudiencesOf(post.Audience).Select(audience => Notice(post, audience) with { Urgent = post.Urgent, Actor = actor }).ToList();
        await notifications.RaiseOneOfAsync(ways, cancellationToken);
    }

    public Task TryOnPhoneAsync(PostRecord post, Guid me, CancellationToken cancellationToken) =>
        notifications.RaiseAsync(Notice(post, NotificationAudience.Person(me)) with { Urgent = post.Urgent, Test = true }, cancellationToken);

    public Task<NotificationReach> ReachAsync(string audience, CancellationToken cancellationToken) =>
        AudiencesOf(audience) is [var only] ? notifications.ReachAsync(only, cancellationToken) : ReachOfSeveralAsync(audience, cancellationToken);

    public Task RecallAsync(Guid id, CancellationToken cancellationToken) => notifications.RecallAsync(Subject(id), cancellationToken);

    public Task WaitingAsync(PostRecord post, Guid actor, CancellationToken cancellationToken) =>
        notifications.RaiseAsync(new Notification(NewsNotifications.WaitingToPublish, NotificationAudience.Holding(NewsPermissions.Publish),
            $"Waiting for you to publish: {post.Title}", $"{post.AuthorName} asks for it to go in church news.", NewsDestinations.PostPath(post.Id.ToString()),
            Subject: WaitingSubject(post.Id)) { Actor = actor }, cancellationToken);

    public Task AskedAsync(PostRecord post, Guid actor, CancellationToken cancellationToken) =>
        notifications.RaiseAsync(new Notification(NewsNotifications.Asked, NotificationAudience.Holding(NewsPermissions.Publish),
            $"{post.FromName} asks for church news: {post.Title}", $"{post.AskedByName} asks for it to go to the whole church.",
            NewsDestinations.PostPath(post.Id.ToString()), Subject: AskSubject(post.Id)) { Actor = actor }, cancellationToken);

    public Task AnsweredAsync(PostRecord post, bool yes, Guid actor, CancellationToken cancellationToken) =>
        post.AskedById is { } asker
            ? notifications.RaiseAsync(new Notification(NewsNotifications.Answered, NotificationAudience.Person(asker),
                yes ? $"In church news: {post.Title}" : $"Not in church news: {post.Title}",
                yes ? "Church staff put your post in church news." : "Church staff kept your post in its ministry's news.",
                post.FromLink) { Actor = actor }, cancellationToken)
            : Task.CompletedTask;

    public async Task SettleAsync(Guid id, CancellationToken cancellationToken)
    {
        await notifications.SettleAsync(WaitingSubject(id), cancellationToken);
        await notifications.SettleAsync(AskSubject(id), cancellationToken);
    }

    public static IReadOnlyList<NotificationAudience> AudiencesOf(string text) =>
        !Audience.TryParse(text, out Audience? audience) || audience == Audience.Everyone ? [NotificationAudience.Everyone]
        : audience == Audience.SignedIn ? [NotificationAudience.SignedIn]
        : audience.IsForRoles ? audience.RoleIds.Select(role => NotificationAudience.Holding(RoleMembership.Of(role))).ToList()
        : audience.Node is { } node ? [NotificationAudience.Holding(new Permission(node, audience.At?.Kind ?? ScopeKind.Global, node), audience.At?.ObjectId)]
        : [NotificationAudience.SignedIn];

    private async Task<NotificationReach> ReachOfSeveralAsync(string audience, CancellationToken cancellationToken)
    {
        int people = 0, phones = 0;
        foreach (NotificationAudience one in AudiencesOf(audience))
        {
            NotificationReach reach = await notifications.ReachAsync(one, cancellationToken);
            (people, phones) = (people + reach.People, phones + reach.Phones);
        }
        return new NotificationReach(people, phones);
    }

    private static Notification Notice(PostRecord post, NotificationAudience audience) =>
        new(NewsNotifications.Post, audience, NewsRules.TellTitleOf(post), NewsRules.TellBodyOf(post), NewsDestinations.PostPath(post.Id.ToString()),
            Subject: Subject(post.Id)) { Actor = null };
}
