using GSBC.ChurchApp.Platform.Api.People;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

internal sealed class RecordedPersonData : IPersonData
{
    public List<Guid> Forgotten { get; } = [];
    public string? Reason { get; set; }
    public int FailuresLeft { get; set; }

    public ValueTask<string?> WhyKeepAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(Reason);

    public Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (FailuresLeft-- > 0)
            throw new InvalidOperationException("A feature couldn't forget them this time.");
        Forgotten.Add(userId);
        return Task.CompletedTask;
    }
}
