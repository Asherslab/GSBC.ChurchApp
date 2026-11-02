using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Ui;

public enum ColourScheme
{
    Automatic,
    Light,
    Dark,
}

public sealed class Appearance(IJSRuntime js)
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
        StaffPalette = chosen.Palette;
        string? css = chosen.Custom is { } custom
            ? ThemeDerivation.Css(custom, CustomSelector, $"{CustomSelector}[data-scheme=\"dark\"]")
            : null;
        await js.InvokeVoidAsync("gsbcTheme.setPalette", chosen.Palette, css);
        Changed?.Invoke();
    }

    private const string CustomSelector = $":root[data-theme=\"{AppearanceSurfaces.CustomPalette}\"]";

    public async Task PreviewAsync(string? palette)
    {
        PreviewPalette = palette == StaffPalette ? null : palette;
        await js.InvokeVoidAsync("gsbcTheme.setPreview", PreviewPalette);
        Changed?.Invoke();
    }
}
