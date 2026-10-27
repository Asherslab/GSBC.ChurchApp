using Gsbc.AppPoc.Layouts;

namespace Gsbc.AppPoc.Native;

public enum Theme { Brand, Year2026 }

public enum Appearance { System, Light, Dark }

public sealed class AppSettings(NativeBridge native)
{
    private const string ThemeKey = "theme";
    private const string AppearanceKey = "appearance";
    private const string LayoutKey = "layout";
    private const string TopicsKey = "topics";
    private const string NotesKeyPrefix = "notes:";
    private const string WelcomedKey = "welcomed";

    private readonly HashSet<string> topics = ["general"];

    public Theme Theme { get; private set; } = Theme.Brand;
    public Appearance Appearance { get; private set; } = Appearance.System;
    public AppLayout Layout { get; private set; } = AppLayout.Bulletin;
    public LayoutOption LayoutOption => LayoutCatalog.For(Layout);
    public IReadOnlySet<string> Topics => topics;

    public event Action? Changed;

    public async Task LoadAsync()
    {
        if (Enum.TryParse<Theme>(await native.GetAsync(ThemeKey), out var theme))
        {
            Theme = theme;
        }
        if (Enum.TryParse<Appearance>(await native.GetAsync(AppearanceKey), out var appearance))
        {
            Appearance = appearance;
        }
        if (Enum.TryParse<AppLayout>(await native.GetAsync(LayoutKey), out var layout))
        {
            Layout = layout;
        }
        if (await native.GetAsync(TopicsKey) is { } saved)
        {
            topics.Clear();
            topics.UnionWith(saved.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
    }

    public async Task SetLayoutAsync(AppLayout layout)
    {
        Layout = layout;
        await native.SetAsync(LayoutKey, layout.ToString());
        Changed?.Invoke();
    }

    public async Task SetThemeAsync(Theme theme)
    {
        Theme = theme;
        await native.SetAsync(ThemeKey, theme.ToString());
        Changed?.Invoke();
    }

    public async Task SetAppearanceAsync(Appearance appearance)
    {
        Appearance = appearance;
        await native.SetAsync(AppearanceKey, appearance.ToString());
        Changed?.Invoke();
    }

    public async Task SetTopicAsync(string topic, bool on)
    {
        if (on)
        {
            topics.Add(topic);
        }
        else
        {
            topics.Remove(topic);
        }
        await native.SetAsync(TopicsKey, string.Join(',', topics));
    }

    public async Task<bool> WelcomedAsync() => await native.GetAsync(WelcomedKey) is not null;

    public ValueTask MarkWelcomedAsync() => native.SetAsync(WelcomedKey, "1");

    public ValueTask<string?> NotesAsync(string id) => native.GetAsync(NotesKeyPrefix + id);

    public ValueTask SaveNotesAsync(string id, string text) => native.SetAsync(NotesKeyPrefix + id, text);
}
