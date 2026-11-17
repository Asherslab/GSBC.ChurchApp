using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed class WalkActions(WalkSession walk, TimeProvider time)
{
    public static readonly TimeSpan SaidFor = TimeSpan.FromSeconds(12);

    private string? _marked;
    private DateTimeOffset _saidAt;

    public event Action? Changed;

    public string? Said { get; private set; }

    public string? Brief { get; private set; }

    public bool CanUndo => _marked is not null;

    public async Task NoJunkMailAsync()
    {
        string? marked = await walk.NoJunkMailAsync();
        if (marked is null)
        {
            Say(walk.Progress?.HasFixes == true ? "No letterbox near you on this street. Stand at it and try again." : "The app needs your location to mark a letterbox.", null);
            return;
        }
        string done = $"Marked No junk mail{(walk.NearestAddress(out _) is { Label.Length: > 0 } address ? $" ({address.Label})" : "")}.";
        Say($"{done} It's skipped from now on.", marked, done);
    }

    public async Task UndoAsync()
    {
        if (_marked is null)
            return;
        await walk.UndoNoJunkMailAsync(_marked);
        Say("Taken back.", null);
    }

    public async Task SpokeAsync()
    {
        await walk.SpokeAsync();
        Say($"Counted. {OutreachWords.Plural(walk.Current?.Conversations ?? 0, "conversation", "conversations")} on this walk.", null);
    }

    public async Task SkipAsync()
    {
        await walk.SkipThisBitAsync();
        Say("Left open for someone else.", null);
    }

    public async Task PauseAsync(bool paused)
    {
        if (walk.Current is { Finished: false } state && state.Paused != paused)
            await walk.PauseAsync(paused);
    }

    public async Task FinishAsync()
    {
        if (walk.Current is { Finished: false })
            await walk.FinishAsync();
        Forget();
    }

    public void Expire()
    {
        if (Said is not null && time.GetUtcNow() - _saidAt > SaidFor)
            Forget();
    }

    private void Forget()
    {
        if (Said is null)
            return;
        Said = null;
        Brief = null;
        _marked = null;
        Changed?.Invoke();
    }

    private void Say(string said, string? marked, string? brief = null)
    {
        Said = said;
        Brief = brief ?? said;
        _marked = marked;
        _saidAt = time.GetUtcNow();
        Changed?.Invoke();
    }
}
