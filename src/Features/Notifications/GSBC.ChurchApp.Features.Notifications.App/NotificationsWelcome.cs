using GSBC.ChurchApp.Features.Notifications.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.Notifications.App;

public enum WelcomeStep
{
    Closed,
    Ask,
    Choose,
}

public sealed class NotificationsWelcome : IOpeningSheet
{
    private const string AnsweredKey = "notifications.welcome.answered";
    private const string NotNow = "not-now";

    private readonly PushRegistration _push;
    private readonly ILocalPreferences _preferences;

    public NotificationsWelcome(PushRegistration push, ILocalPreferences preferences)
    {
        _push = push;
        _preferences = preferences;
        push.Changed += OnPermissionChanged;
    }

    public WelcomeStep Step { get; private set; } = WelcomeStep.Closed;

    public event Action? Changed;

    public async Task<bool> OfferAsync()
    {
        if (_push.Permission != PushRegistration.Undecided || await _preferences.GetAsync(AnsweredKey) is not null)
            return false;
        Go(WelcomeStep.Ask);
        return true;
    }

    public async Task TurnOnAsync()
    {
        Go(await _push.TurnOnAsync() == PushRegistration.Granted ? WelcomeStep.Choose : WelcomeStep.Closed);
    }

    public async Task NotNowAsync()
    {
        await _preferences.SetAsync(AnsweredKey, NotNow);
        Go(WelcomeStep.Closed);
    }

    public void Close() => Go(WelcomeStep.Closed);

    private void OnPermissionChanged()
    {
        if (Step != WelcomeStep.Ask || _push.Permission == PushRegistration.Undecided)
            return;
        Go(_push.Permission == PushRegistration.Granted ? WelcomeStep.Choose : WelcomeStep.Closed);
    }

    private void Go(WelcomeStep step)
    {
        Step = step;
        Changed?.Invoke();
    }
}
