using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Api.News;

public interface IChurchNews
{
    Task AskAsync(SharedPost post, Guid askedBy, CancellationToken cancellationToken);

    Task ShareAsync(SharedPost post, Guid sharedBy, bool tellPeople, CancellationToken cancellationToken);

    Task ChangedAsync(SharedPost post, CancellationToken cancellationToken);

    Task WithdrawAsync(string source, Guid sourceId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, SharedState>> StatesAsync(string source, IReadOnlyCollection<Guid> sourceIds, CancellationToken cancellationToken);
}

public sealed record SharedPost(
    string Source, Guid SourceId, Guid FromId, string FromName, string Title, RichText Body, string AuthorName, DateTime PostedAt, DateTime? EditedAt, string Link);

public enum SharedState
{
    None,
    Asked,
    Shared,
    Declined,
}

internal sealed class NoChurchNews : IChurchNews
{
    public Task AskAsync(SharedPost post, Guid askedBy, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ShareAsync(SharedPost post, Guid sharedBy, bool tellPeople, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ChangedAsync(SharedPost post, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task WithdrawAsync(string source, Guid sourceId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IReadOnlyDictionary<Guid, SharedState>> StatesAsync(string source, IReadOnlyCollection<Guid> sourceIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, SharedState>>(new Dictionary<Guid, SharedState>());
}

public interface ISharedPostSource
{
    string Source { get; }

    Task StateChangedAsync(Guid sourceId, SharedState state, CancellationToken cancellationToken);
}
