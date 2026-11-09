using GSBC.ChurchApp.Platform.Client.Audio;
using GSBC.ChurchApp.Platform.Ui.Tabs;

namespace GSBC.ChurchApp.Features.Sermons.App;

public sealed class SermonsTabIcon : ILiveTabIcon, IDisposable
{
    private const string Playing = "pause";
    private const string NotPlaying = "play";

    private readonly IAudioPlayer _player;
    private bool _playing;

    public SermonsTabIcon(IAudioPlayer player)
    {
        _player = player;
        _playing = player.State.Playing;
        player.Changed += OnChanged;
    }

    public string Route => SermonsAppExtensions.Route;

    public string Icon => _playing ? Playing : NotPlaying;

    public event Action? Changed;

    private void OnChanged()
    {
        if (_player.State.Playing == _playing)
            return;
        _playing = _player.State.Playing;
        Changed?.Invoke();
    }

    public void Dispose() => _player.Changed -= OnChanged;
}
