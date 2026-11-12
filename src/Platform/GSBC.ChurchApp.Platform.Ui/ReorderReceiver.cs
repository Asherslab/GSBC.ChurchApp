using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Ui;

public sealed class ReorderReceiver(Func<int, int, Task> preview, Func<int, int, Task> moved)
{
    [JSInvokable]
    public Task Preview(int from, int to) => preview(from, to);

    [JSInvokable]
    public Task Moved(int from, int to) => moved(from, to);
}
