using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Ui.Surfaces;

public sealed class SurfaceViews(IEnumerable<SurfaceViewRegistration> registrations)
{
    private readonly Dictionary<string, Func<ArrangedSurface, RenderFragment>> _views =
        registrations.GroupBy(registration => registration.Surface.Key).ToDictionary(group => group.Key, group => group.Last().View);

    public bool Has(Surface surface) => _views.ContainsKey(surface.Key);

    public RenderFragment? For(ArrangedSurface entry) => _views.GetValueOrDefault(entry.Surface.Key)?.Invoke(entry);
}
