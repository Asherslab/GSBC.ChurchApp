namespace GSBC.ChurchApp.Platform.Api.Email;

public interface IAccountEmails
{
    ValueTask<IReadOnlyList<string>> EmailsOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}

internal sealed class NoAccountEmails : IAccountEmails
{
    public ValueTask<IReadOnlyList<string>> EmailsOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<string>>([]);
}
