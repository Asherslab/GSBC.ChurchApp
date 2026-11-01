namespace GSBC.ChurchApp.Platform.Client.Sessions;

public sealed class SessionSignal
{
    public event Func<Task>? Changed;

    public async Task RaiseAsync()
    {
        if (Changed is { } changed)
            await Task.WhenAll(changed.GetInvocationList().Cast<Func<Task>>().Select(handler => handler()));
    }
}
