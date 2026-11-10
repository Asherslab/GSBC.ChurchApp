using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Api.Chats;

public interface IChatPlaces
{
    ValueTask<IReadOnlyDictionary<Guid, ChatPlace>> PlacesAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<ChatPlace>> AllAsync(CancellationToken cancellationToken);
}

public sealed record ChatPlace(Guid MinistryId, string Name, bool ChatsOn, IReadOnlyDictionary<string, Audience> Audiences);

internal sealed class NoChatPlaces : IChatPlaces
{
    public ValueTask<IReadOnlyDictionary<Guid, ChatPlace>> PlacesAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, ChatPlace>>(new Dictionary<Guid, ChatPlace>());

    public ValueTask<IReadOnlyList<ChatPlace>> AllAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<ChatPlace>>([]);
}
