using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.News.Api.Tests;

internal sealed class RecordedNotifications : INotifications
{
    public List<Notification> Raised { get; } = [];
    public List<string> Settled { get; } = [];
    public List<string> Recalled { get; } = [];

    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken)
    {
        Raised.Add(NotificationLinks.Checked(notification));
        return Task.CompletedTask;
    }

    public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken)
    {
        Raised.AddRange(mostSpecificFirst.Select(NotificationLinks.Checked));
        return Task.CompletedTask;
    }

    public Task SettleAsync(string subject, CancellationToken cancellationToken)
    {
        Settled.Add(subject);
        return Task.CompletedTask;
    }

    public Task RecallAsync(string subject, CancellationToken cancellationToken)
    {
        Recalled.Add(subject);
        return Task.CompletedTask;
    }

    public Task CollectAsync(NotificationDigestItem item, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ForgetDigestAsync(NotificationKind kind, string group, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class KnownPeople : IPeopleIndex
{
    public Dictionary<Guid, string> Names { get; } = [];

    public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(people.Where(Names.ContainsKey).ToDictionary(person => person, person => Names[person]));

    public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<IReadOnlySet<Guid>> NotInvitedYetAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) => throw new NotSupportedException();
}

internal sealed class LibraryMedia : IMediaReferences, IMediaSystemFolders
{
    public Dictionary<string, List<string>> Uses { get; } = [];

    public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaRef>>(claimed.Select(media => new MediaRef
        {
            Id = media.Id, Key = $"library/{media.Id}", ContentType = media.ContentType, FileName = media.FileName, Url = $"https://media.example/{media.Id}",
        }).ToList());

    public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken)
    {
        Uses[owner] = verified.Select(media => media.Id).ToList();
        return Task.CompletedTask;
    }

    public Task FileAsync(MediaSystemArea area, string? scopeId, string scopeName, IReadOnlyCollection<MediaRef> media, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<MediaRef> KeepAsync(MediaSystemArea area, KeptFile file, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<MediaRef> KeepStoredAsync(MediaSystemArea area, StoredFile file, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task NameAsync(MediaSystemArea area, string mediaId, string fileName, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ForgetAsync(MediaSystemArea area, IReadOnlyCollection<string> mediaIds, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ForgetUnusedAsync(MediaSystemArea area, DateTime uploadedBefore, CancellationToken cancellationToken) => Task.CompletedTask;
}
