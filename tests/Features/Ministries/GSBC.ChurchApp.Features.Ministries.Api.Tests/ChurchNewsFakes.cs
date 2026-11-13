using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

internal sealed class KeptPostMedia : IMediaReferences, IMediaSystemFolders, IMediaAudiences
{
    public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaRef>>([.. claimed]);

    public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task FileAsync(MediaSystemArea area, string? scopeId, string scopeName, IReadOnlyCollection<MediaRef> media, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<MediaRef> KeepAsync(MediaSystemArea area, KeptFile file, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<MediaRef> KeepStoredAsync(MediaSystemArea area, StoredFile file, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task NameAsync(MediaSystemArea area, string mediaId, string fileName, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ForgetAsync(MediaSystemArea area, IReadOnlyCollection<string> mediaIds, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ForgetUnusedAsync(MediaSystemArea area, DateTime uploadedBefore, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<bool> ShareAsync(MediaSystemArea area, string mediaId, string owner, string? permissionNode, string? scopeId, CancellationToken cancellationToken) =>
        Task.FromResult(true);
}

internal sealed class RecordedChurchNews : IChurchNews
{
    public List<(string What, SharedPost? Post, Guid SourceId)> Calls { get; } = [];

    public Task AskAsync(SharedPost post, Guid askedBy, CancellationToken cancellationToken) => Add("ask", post);

    public Task ShareAsync(SharedPost post, Guid sharedBy, bool tellPeople, CancellationToken cancellationToken) => Add("share", post);

    public Task ChangedAsync(SharedPost post, CancellationToken cancellationToken) => Add("changed", post);

    public Task WithdrawAsync(string source, Guid sourceId, CancellationToken cancellationToken)
    {
        Calls.Add(("withdraw", null, sourceId));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<Guid, SharedState>> StatesAsync(string source, IReadOnlyCollection<Guid> sourceIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, SharedState>>(new Dictionary<Guid, SharedState>());

    private Task Add(string what, SharedPost post)
    {
        Calls.Add((what, post, post.SourceId));
        return Task.CompletedTask;
    }
}
