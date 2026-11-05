namespace GSBC.ChurchApp.Platform.Ui;

public sealed class NowPlayingBar
{
    public string? HiddenFor { get; private set; }

    public bool Hidden => HiddenFor is not null;

    public event Action? Changed;

    public void Hide(string itemId)
    {
        HiddenFor = itemId;
        Changed?.Invoke();
    }

    public void Show()
    {
        if (HiddenFor is null)
            return;
        HiddenFor = null;
        Changed?.Invoke();
    }

    public void ShowFor(string itemId)
    {
        if (HiddenFor == itemId)
            Show();
    }
}
