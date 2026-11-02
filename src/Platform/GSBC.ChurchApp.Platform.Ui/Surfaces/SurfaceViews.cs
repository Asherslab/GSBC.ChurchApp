using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Ui.Surfaces;

public sealed class SurfaceViews
{
    private readonly Dictionary<string, Func<ArrangedSurface, RenderFragment>> _views = [];

    public SurfaceViews Add(Surface surface, Func<ArrangedSurface, RenderFragment> view)
    {
        _views[surface.Key] = view;
        return this;
    }

    public RenderFragment? For(ArrangedSurface entry) => _views.GetValueOrDefault(entry.Surface.Key)?.Invoke(entry);
}
