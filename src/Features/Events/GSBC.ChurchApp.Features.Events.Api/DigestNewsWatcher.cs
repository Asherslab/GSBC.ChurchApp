using GSBC.ChurchApp.Platform.Api.News;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class DigestNewsWatcher(EventsPublisher publisher) : INewsWatcher
{
    public async Task PostsChangedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (await publisher.SentAnyAsync(ids, cancellationToken))
            await publisher.PublishAsync(cancellationToken);
    }
}
