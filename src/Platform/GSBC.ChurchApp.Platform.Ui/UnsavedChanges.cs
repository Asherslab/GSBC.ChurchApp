namespace GSBC.ChurchApp.Platform.Ui;

public sealed class UnsavedChanges
{
    private readonly List<UnsavedGuard> _guards = [];

    public bool Any => _guards.Any(guard => guard.HasChanges);

    public async Task<bool> MayLeaveAsync()
    {
        foreach (UnsavedGuard guard in _guards.ToList())
            if (guard.HasChanges && !await guard.AskAsync())
                return false;
        return true;
    }

    internal void Add(UnsavedGuard guard) => _guards.Add(guard);

    internal void Remove(UnsavedGuard guard) => _guards.Remove(guard);
}
