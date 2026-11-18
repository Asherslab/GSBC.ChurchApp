namespace GSBC.ChurchApp.Platform.Ui;

public sealed class FrameBudget(TimeProvider time)
{
    private static readonly TimeSpan Slice = TimeSpan.FromMilliseconds(8);
    private static readonly TimeSpan BrowserTurn = TimeSpan.FromMilliseconds(1);

    private long _started = time.GetTimestamp();

    public ValueTask BreatheAsync() => time.GetElapsedTime(_started) < Slice ? ValueTask.CompletedTask : PauseAsync();

    public async Task<T> RunAsync<T>(IEnumerable<T?> steps) where T : class
    {
        foreach (T? step in steps)
        {
            if (step is not null)
                return step;
            await BreatheAsync();
        }
        throw new InvalidOperationException("The work ended without a result.");
    }

    private async ValueTask PauseAsync()
    {
        await Task.Delay(BrowserTurn, time);
        _started = time.GetTimestamp();
    }
}
