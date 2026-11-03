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

public sealed class NotificationsWelcome(PushRegistration push, ILocalPreferences preferences) : IOpeningSheet
{
    private const string AnsweredKey = "notifications.welcome.answered";
    private const string NotNow = "not-now";

    public WelcomeStep Step { get; private set; } = WelcomeStep.Closed;

    public event Action? Changed;

    public async Task<bool> OfferAsync()
    {
        if (push.Permission != PushRegistration.Undecided || await preferences.GetAsync(AnsweredKey) is not null)
            return false;
        Go(WelcomeStep.Ask);
        return true;
    }

    public async Task TurnOnAsync()
    {
        Go(await push.TurnOnAsync() == PushRegistration.Granted ? WelcomeStep.Choose : WelcomeStep.Closed);
    }

    public async Task NotNowAsync()
    {
        await preferences.SetAsync(AnsweredKey, NotNow);
        Go(WelcomeStep.Closed);
    }

    public void Close() => Go(WelcomeStep.Closed);

    private void Go(WelcomeStep step)
    {
        Step = step;
        Changed?.Invoke();
    }
}
