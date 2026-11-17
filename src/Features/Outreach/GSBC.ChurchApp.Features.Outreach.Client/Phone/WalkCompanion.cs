using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Location;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed class WalkCompanion(
    WalkSession walk,
    WalkActions actions,
    ILocationSession location,
    IWalkActivity activity,
    NavigationManager navigation,
    TimeProvider time) : IClientStartupTask, IDisposable
{
    public const string DoorQuery = "door";
    private static readonly TimeSpan TickEvery = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan RefreshEvery = TimeSpan.FromMinutes(1);

    private readonly SemaphoreSlim _busy = new(1, 1);
    private ITimer? _timer;
    private WalkActivityState? _shown;
    private DateTimeOffset _shownAt;
    private bool _showing = true;
    private bool _again;

    public event Action? NewStreet;

    public async Task RunAsync()
    {
        location.FixArrived += OnSignal;
        activity.TapArrived += OnSignal;
        actions.Changed += OnSignal;
        walk.Changed += OnSignal;
        _timer = time.CreateTimer(_ => OnSignal(), null, TickEvery, TickEvery);
        await walk.LoadAsync();
        await CatchUpAsync();
    }

    private void OnSignal() => _ = CatchUpAsync();

    public async Task CatchUpAsync()
    {
        _again = true;
        if (!await _busy.WaitAsync(0))
            return;
        try
        {
            while (_again)
            {
                _again = false;
                await TapsAsync();
                int before = walk.Progress?.Current ?? -1;
                if (await walk.DrainAsync())
                    NewStreet?.Invoke();
                actions.Expire();
                await ShowAsync(Alert(before));
            }
        }
        finally
        {
            _busy.Release();
        }
    }

    private async Task TapsAsync()
    {
        IReadOnlyList<WalkTap> taps = await activity.TapsAsync(0);
        foreach (WalkTap tap in taps)
        {
            await walk.DrainAsync(tap.At);
            await ApplyAsync(tap.Kind);
        }
        if (taps.Count > 0)
            await activity.ClearTapsAsync(taps[^1].Seq);
    }

    private async Task ApplyAsync(WalkTapKind kind)
    {
        switch (kind)
        {
            case WalkTapKind.NoJunkMail:
                await actions.NoJunkMailAsync();
                break;
            case WalkTapKind.Spoke:
                await actions.SpokeAsync();
                break;
            case WalkTapKind.Undo:
                await actions.UndoAsync();
                break;
            case WalkTapKind.Pause:
                await actions.PauseAsync(true);
                break;
            case WalkTapKind.Resume:
                await actions.PauseAsync(false);
                break;
            case WalkTapKind.Finish:
                await actions.FinishAsync();
                navigation.NavigateTo(OutreachLinks.Walking);
                break;
            case WalkTapKind.Door:
                navigation.NavigateTo($"{OutreachLinks.Walking}?{DoorQuery}=1");
                break;
            case WalkTapKind.Open:
                navigation.NavigateTo(OutreachLinks.Walking);
                break;
        }
    }

    private string? Alert(int before)
    {
        if (walk.Progress is not { } progress || before < 0 || progress.Current == before || progress.Now is not { } now || before >= progress.Steps.Count)
            return null;
        SavedStep from = progress.Steps[before].Step;
        return from.Street != now.Step.Street && WalkActivityWords.IsTurn(from, now.Step)
            ? $"{OutreachPhoneWords.Side(now.Step.Delivers)} · {OutreachPhoneWords.Remaining(now.Step.Letterboxes - now.Done)}"
            : null;
    }

    private async Task ShowAsync(string? alert)
    {
        if (walk.Current is not { Finished: false } state)
        {
            if (_showing)
                await activity.EndAsync();
            _showing = false;
            _shown = null;
            return;
        }
        WalkActivityState shown = WalkActivityWords.Of(state, walk.Progress, actions.Brief, actions.CanUndo, time.GetUtcNow().UtcDateTime);
        if (alert is null && shown == _shown && time.GetUtcNow() - _shownAt < RefreshEvery)
            return;
        await activity.ShowAsync(state.Name, shown, alert);
        _shown = shown;
        _shownAt = time.GetUtcNow();
        _showing = true;
    }

    public void Dispose()
    {
        location.FixArrived -= OnSignal;
        activity.TapArrived -= OnSignal;
        actions.Changed -= OnSignal;
        walk.Changed -= OnSignal;
        _timer?.Dispose();
    }
}
