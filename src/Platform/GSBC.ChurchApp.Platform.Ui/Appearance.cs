using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Ui;

public enum ColourScheme
{
    Automatic,
    Light,
    Dark,
}

public sealed class Appearance(IJSRuntime js, IMediaFiles files)
{
    public ColourScheme Scheme { get; private set; } = ColourScheme.Automatic;

    public string StaffPalette { get; private set; } = AppearanceSurfaces.Brand;

    public string? PreviewPalette { get; private set; }

    public string Palette => PreviewPalette ?? StaffPalette;

    public event Action? Changed;

    public async Task LoadAsync()
    {
        Scheme = Enum.TryParse(await js.InvokeAsync<string>("gsbcTheme.appearance"), ignoreCase: true, out ColourScheme scheme) ? scheme : ColourScheme.Automatic;
        PreviewPalette = await js.InvokeAsync<string?>("gsbcTheme.preview");
        Changed?.Invoke();
    }

    public async Task SetSchemeAsync(ColourScheme scheme)
    {
        Scheme = scheme;
        await js.InvokeVoidAsync("gsbcTheme.setAppearance", scheme.ToString().ToLowerInvariant());
        Changed?.Invoke();
    }

    public async Task UseStaffThemeAsync(AppearanceSurfaces.ChosenTheme chosen)
    {
        StaffPalette = chosen.Custom is null ? chosen.Palette : chosen.ChosenValue;
        string? css = chosen.Custom is { } custom
            ? ThemeDerivation.Css(custom, CustomSelector, $"{CustomSelector}[data-scheme=\"dark\"]", await FontSourcesAsync(files, custom))
            : null;
        await js.InvokeVoidAsync("gsbcTheme.setPalette", chosen.Palette, css);
        Changed?.Invoke();
    }

    public static async Task<IReadOnlyDictionary<string, string>> FontSourcesAsync(IMediaFiles files, ThemeSpec theme)
    {
        Dictionary<string, string> sources = [];
        foreach (Contracts.Media.MediaRef font in ThemeDerivation.FontFiles(theme))
        {
            if (await files.SourceOfAsync(font) is { } source)
                sources[font.Key] = source;
        }
        return sources;
    }

    private const string CustomSelector = $":root[data-theme=\"{AppearanceSurfaces.CustomPalette}\"]";

    public async Task PreviewAsync(string? palette, ThemeSpec? made = null)
    {
        PreviewPalette = palette == StaffPalette ? null : palette;
        string? css = PreviewPalette is not null && made is not null
            ? ThemeDerivation.Css(made, PreviewSelector, $"{PreviewSelector}[data-scheme=\"dark\"]", await FontSourcesAsync(files, made))
            : null;
        await js.InvokeVoidAsync("gsbcTheme.setPreview", PreviewPalette, css);
        Changed?.Invoke();
    }

    private const string PreviewSelector = $":root[data-theme=\"{AppearanceSurfaces.PreviewPalette}\"]";
}
