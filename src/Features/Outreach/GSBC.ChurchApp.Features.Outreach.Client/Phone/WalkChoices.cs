using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Routing;
using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed record WalkLength(int Minutes, double Kilometres)
{
    public static WalkLength Default { get; } = new(60, 0);

    public static IReadOnlyList<WalkLength> Times { get; } = [new(30, 0), new(60, 0), new(120, 0)];

    public static IReadOnlyList<WalkLength> Distances { get; } = [new(0, 1), new(0, 3), new(0, 5), new(0, 8)];

    public bool ByTime => Minutes > 0;

    public WalkBudget Budget => ByTime ? new WalkBudget(Minutes) : new WalkBudget(Kilometres: Kilometres);

    public string Label => ByTime ? OutreachPhoneWords.Duration(TimeSpan.FromMinutes(Minutes)) : $"{Kilometres.ToString("0.#", CultureInfo.InvariantCulture)} km";

    public string Query => ByTime ? $"minutes={Minutes}" : $"km={Kilometres.ToString("0.#", CultureInfo.InvariantCulture)}";

    public static WalkLength Parse(int? minutes, double? kilometres) =>
        minutes is > 0 and <= 600 ? new(minutes.Value, 0) : kilometres is > 0 and <= 30 ? new(0, kilometres.Value) : Default;
}

public sealed class WalkChoices(ILocalPreferences preferences)
{
    private const string CarryingKey = "outreach.carrying";
    private const string FirstNameKey = "outreach.first-name";
    private const string KnockingForKey = "outreach.knocking-for";

    public async Task<IReadOnlyList<string>> CarryingAsync() =>
        (await preferences.GetAsync(CarryingKey) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);

    public Task CarryAsync(IEnumerable<string> campaigns) => preferences.SetAsync(CarryingKey, string.Join(',', campaigns)).AsTask();

    public async Task<string?> KnockingForAsync() => await preferences.GetAsync(KnockingForKey);

    public Task KnockForAsync(string campaign) => preferences.SetAsync(KnockingForKey, campaign).AsTask();

    public async Task<string> FirstNameAsync() => await preferences.GetAsync(FirstNameKey) ?? "";

    public Task RememberFirstNameAsync(string name) => preferences.SetAsync(FirstNameKey, name.Trim()).AsTask();
}
