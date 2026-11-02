using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Sessions;

namespace GSBC.ChurchApp.Features.Media.Client;

internal sealed class PrivateMediaGuard(SessionSignal signal, IMediaFiles files) : IClientStartupTask
{
    public Task RunAsync()
    {
        signal.Changed += async () => await files.ForgetPrivateAsync();
        return Task.CompletedTask;
    }
}
