using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Analytics.Client;

public sealed class AnalyticsConsent(IServiceProvider services)
{
    private const string PreferenceKey = "analytics.enabled";
    private const string Off = "off";

    private readonly ILocalPreferences? _preferences = services.GetService<ILocalPreferences>();

    public bool Enabled { get; private set; } = true;

    public bool CanChange => _preferences is not null;

    public event Action? Changed;

    public async Task LoadAsync()
    {
        if (_preferences is not null)
            Enabled = await _preferences.GetAsync(PreferenceKey) != Off;
    }

    public async Task SetAsync(bool enabled)
    {
        if (_preferences is null || enabled == Enabled)
            return;

        await _preferences.SetAsync(PreferenceKey, enabled ? null : Off);
        Enabled = enabled;
        if (!enabled && services.GetService<IOutbox>() is { IsAvailable: true } outbox)
            await outbox.DiscardQuietAsync();
        Changed?.Invoke();
    }
}
