namespace GSBC.ChurchApp.Platform.Api.People;

public interface IPersonData
{
    ValueTask<string?> WhyKeepAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult<string?>(null);

    Task ForgetAsync(Guid userId, CancellationToken cancellationToken);
}
