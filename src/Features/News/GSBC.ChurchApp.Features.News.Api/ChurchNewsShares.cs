using GSBC.ChurchApp.Platform.Api.News;

namespace GSBC.ChurchApp.Features.News.Api;

internal sealed class ChurchNewsShares(NewsDesk desk) : IChurchNews
{
    public Task AskAsync(SharedPost post, Guid askedBy, CancellationToken cancellationToken) =>
        desk.OfferAsync(post, askedBy, asked: true, tell: false, cancellationToken);

    public Task ShareAsync(SharedPost post, Guid sharedBy, bool tellPeople, CancellationToken cancellationToken) =>
        desk.OfferAsync(post, sharedBy, asked: false, tellPeople, cancellationToken);

    public Task ChangedAsync(SharedPost post, CancellationToken cancellationToken) => desk.SourceChangedAsync(post, cancellationToken);

    public Task WithdrawAsync(string source, Guid sourceId, CancellationToken cancellationToken) => desk.WithdrawAsync(source, sourceId, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, SharedState>> StatesAsync(string source, IReadOnlyCollection<Guid> sourceIds, CancellationToken cancellationToken) =>
        desk.StatesAsync(source, sourceIds, cancellationToken);
}
