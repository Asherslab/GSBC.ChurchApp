using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Ui;

public sealed class WindowChannel : IAsyncDisposable
{
    private readonly Func<string, Task> _heard;

    internal WindowChannel(Func<string, Task> heard)
    {
        _heard = heard;
        Receiver = DotNetObjectReference.Create(this);
    }

    internal DotNetObjectReference<WindowChannel> Receiver { get; }

    internal IJSObjectReference? Subscription { get; set; }

    [JSInvokable]
    public Task OnHeard(string message) => _heard(message);

    public async ValueTask DisposeAsync()
    {
        if (Subscription is not null)
        {
            await Subscription.InvokeVoidAsync("close");
            await Subscription.DisposeAsync();
        }
        Receiver.Dispose();
    }
}
