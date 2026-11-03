namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed class PushSignal
{
    private readonly SemaphoreSlim _wake = new(0);

    public void Wake()
    {
        if (_wake.CurrentCount == 0)
            _wake.Release();
    }

    public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _wake.WaitAsync(timeout, cancellationToken);
}
