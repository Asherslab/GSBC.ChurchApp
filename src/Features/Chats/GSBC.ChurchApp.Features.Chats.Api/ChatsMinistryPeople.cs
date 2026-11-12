using GSBC.ChurchApp.Platform.Api.Ministries;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatsMinistryPeople(ChatPublisher publisher) : IMinistryPeopleChanged
{
    public Task ChangedAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken) => publisher.PublishInAsync(ministryIds, cancellationToken);
}
