namespace GSBC.ChurchApp.Platform.Api.Access;

public interface ISignedInSessions
{
    ValueTask<bool> IsCurrentAsync(string sessionId, CancellationToken cancellationToken);
}

internal sealed class UncheckedSessions : ISignedInSessions
{
    public ValueTask<bool> IsCurrentAsync(string sessionId, CancellationToken cancellationToken) => ValueTask.FromResult(true);
}
