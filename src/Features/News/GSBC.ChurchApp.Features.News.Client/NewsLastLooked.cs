using System.Globalization;
using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.News.Client;

public sealed class NewsLastLooked(ILocalPreferences preferences, TimeProvider time)
{
    private const string Key = "news.last-looked";

    public async Task<DateTime?> LoadAsync() =>
        DateTime.TryParse(await preferences.GetAsync(Key), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime at) ? at : null;

    public ValueTask LookedAsync() => preferences.SetAsync(Key, time.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
}
