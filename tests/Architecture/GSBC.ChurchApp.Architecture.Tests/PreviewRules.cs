using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Ui;
using GSBC.ChurchApp.Platform.Ui.Surfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class PreviewRules
{
    private const string AppHome = "src/Hosts/App/GSBC.ChurchApp.Hosts.App/Pages/Home.razor";

    [Fact]
    public void TheAppsHomeDrawsWhatDraftsChangeThroughTheScreenThePreviewShares()
    {
        string home = File.ReadAllText(Path.Combine(RepoRoot.Path, AppHome));

        Assert.Contains("<HomeScreen", home);
        Assert.DoesNotContain("<HubTiles", home);
        Assert.DoesNotContain("<SurfaceView", home);
    }

    [Fact]
    public void EveryHubSurfaceHasAViewSoItShowsOnThePhoneAndInThePreview()
    {
        SurfaceViews views = new ServiceCollection().AddPlatformUi().BuildServiceProvider().GetRequiredService<SurfaceViews>();

        List<string> missing = HubSurfaces.All.Surfaces
            .Where(surface => !HubSurfaces.IsArrangement(surface) && views.For(new ArrangedSurface(surface, null)) is null)
            .Select(surface => surface.Key)
            .ToList();

        Assert.Empty(missing);
    }
}
