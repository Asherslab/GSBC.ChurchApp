using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public static class ThemeNames
{
    public static string Of(string palette, IEnumerable<ArrangedSurface> appearance) =>
        ThemeChoiceSetting.IsCustom(palette, out string id) && appearance.FirstOrDefault(entry => entry.Placement?.Id == id) is { } made
            ? made.Values.Get(AppearanceSurfaces.CustomTheme.Name)
            : "the church's brand";

    public static string Chosen(IEnumerable<ArrangedSurface> appearance, Viewer viewer, DateTime at)
    {
        List<ArrangedSurface> shown = appearance.Where(entry => entry.IsShownTo(viewer, at)).ToList();
        return Of(AppearanceSurfaces.ChosenFrom(shown).ChosenValue, shown);
    }
}
