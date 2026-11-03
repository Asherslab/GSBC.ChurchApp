namespace GSBC.ChurchApp.Platform.Client.Preferences;

public interface ILocalPreferences
{
    ValueTask<string?> GetAsync(string key);

    ValueTask SetAsync(string key, string? value);
}
