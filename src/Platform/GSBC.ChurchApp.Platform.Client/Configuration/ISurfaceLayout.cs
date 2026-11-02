using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Client.Configuration;

public interface ISurfaceLayout
{
    event Action? Changed;

    Task<IReadOnlyList<ArrangedSurface>> ShownAsync(SurfaceArea area, string? scopeId = null);
}
