using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static GSBC.ChurchApp.Features.Configuration.Api.PlacementEditor;
using CustomTheme = GSBC.ChurchApp.Platform.Contracts.Configuration.AppearanceSurfaces.CustomTheme;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class ThemeSeeding(IServiceScopeFactory scopes, ILogger<ThemeSeeding> logger) : BackgroundService
{
    public const string Key = "themes";

    private sealed record Seed(string Id, string Name, string Font, ThemeKeys Light, ThemeKeys Dark, Dictionary<string, string> LightOverrides, Dictionary<string, string> DarkOverrides);

    private static readonly Seed[] Seeds =
    [
        new(AppearanceSurfaces.Seeded.BrandId, "The church's brand", "montserrat",
            new ThemeKeys("#2a3d5e", "#2a3d5e", "#00877c", "#e7ae74", "#f4efe7"),
            new ThemeKeys("#283c62", "#2ec4b6", "#2ec4b6", "#e7ae74", "#1d2840"),
            new() { ["ink"] = "#18233a", ["ink-2"] = "#545e70", ["accent"] = "#00776d", ["masthead-note"] = "#d5c9b7", ["on-live"] = "#ffffff", ["marker-ink"] = "#8a5422" },
            new() { ["ink"] = "#f2ede4", ["ink-2"] = "#b2b8c5", ["masthead-note"] = "#d5c9b7", ["marker-ink"] = "#f0c493" }),
        new(AppearanceSurfaces.Seeded.Year2026Id, "2026 theme", "dm-serif-text",
            new ThemeKeys("#4a7355", "#3f6449", "#4a7355", "#c9bd4a", "#f3f2e2"),
            new ThemeKeys("#24382a", "#a6cf9e", "#a6cf9e", "#c9bd4a", "#1a261d"),
            new() { ["ink"] = "#1e2a22", ["ink-2"] = "#4f5e53", ["accent"] = "#3b6847", ["masthead-note"] = "#fffcd1", ["marker-ink"] = "#5e5718" },
            new() { ["ink"] = "#f2ede4", ["ink-2"] = "#b2b8c5", ["masthead-note"] = "#fffcd1", ["marker-ink"] = "#ece48f" }),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            if (await SeedOnceAsync(scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>(), scope.ServiceProvider.GetRequiredService<PlacementEditor>(), stoppingToken))
                logger.LogInformation("Seeded the church's brand and the 2026 theme as themes staff can change");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Seeding the themes failed");
        }
    }

    public static async Task<bool> SeedOnceAsync(ConfigurationDbContext db, PlacementEditor placements, CancellationToken cancellationToken)
    {
        if (await db.Seeds.AnyAsync(seed => seed.Key == Key, cancellationToken))
            return false;

        SurfaceArea area = AppearanceSurfaces.Area;
        List<PlacementRecord> existing = await placements.InArea(area, null).ToListAsync(cancellationToken);
        List<Change> changes = [];
        foreach (Seed seed in Seeds.Where(seed => existing.All(placement => placement.Id != Guid.Parse(seed.Id))))
        {
            PlacementRecord record = placements.Create(area, null, CustomTheme.Surface, Guid.Parse(seed.Id), existing);
            changes.Add(new Change(record, null, StateOf(seed, existing.Count(placement => placement.Surface == CustomTheme.Surface.Key))));
        }

        PlacementRecord? theme = existing.FirstOrDefault(placement => placement.Surface == AppearanceSurfaces.Theme.Surface.Key);
        string chosen = theme is null ? AppearanceSurfaces.Theme.Palette.Default : new SettingValues(theme.Values).Get(AppearanceSurfaces.Theme.Palette);
        if (theme is null)
        {
            theme = placements.Create(area, null, AppearanceSurfaces.Theme.Surface, Guid.CreateVersion7(), existing);
            changes.Add(new Change(theme, null, new PlacementState(AppearanceSurfaces.Theme.Surface.Key, 0, false, Audience.Everyone.Text, null, null,
                new SortedDictionary<string, string> { [AppearanceSurfaces.Theme.Palette.Key] = chosen })));
        }
        else if (theme.Values.GetValueOrDefault(AppearanceSurfaces.Theme.Palette.Key) != chosen)
        {
            PlacementState before = PlacementState.Of(theme);
            changes.Add(new Change(theme, before, before with { Values = new SortedDictionary<string, string>(theme.Values) { [AppearanceSurfaces.Theme.Palette.Key] = chosen } }));
        }

        db.Seeds.Add(new SeedRecord { Key = Key, SeededAt = DateTime.UtcNow });
        await placements.CommitAsync(Guid.Empty, changes, rollsBack: null, draft: null, cancellationToken);
        return true;
    }

    private static PlacementState StateOf(Seed seed, int order)
    {
        ThemeFont font = AppearanceSurfaces.Fonts.Single(candidate => candidate.Key == seed.Font);
        ThemeFont body = AppearanceSurfaces.Fonts.Single(candidate => candidate.Key == "montserrat");
        SortedDictionary<string, string> values = new()
        {
            [CustomTheme.Name.Key] = seed.Name,
            [CustomTheme.Font.Key] = CustomTheme.Font.Format(font),
            [CustomTheme.BodyFont.Key] = CustomTheme.BodyFont.Format(body),
            [CustomTheme.Overrides.Key] = CustomTheme.Overrides.Format(
                seed.LightOverrides.Select(pair => (ColourOverridesSetting.RoleKey(ThemeScheme.Light, pair.Key), pair.Value))
                    .Concat(seed.DarkOverrides.Select(pair => (ColourOverridesSetting.RoleKey(ThemeScheme.Dark, pair.Key), pair.Value)))
                    .ToDictionary()),
        };
        string[] light = [seed.Light.Masthead, seed.Light.Primary, seed.Light.Accent, seed.Light.Marker, seed.Light.Tint];
        string[] dark = [seed.Dark.Masthead, seed.Dark.Primary, seed.Dark.Accent, seed.Dark.Marker, seed.Dark.Tint];
        foreach ((HexColourSetting key, string colour) in CustomTheme.LightKeys.Zip(light).Concat(CustomTheme.DarkKeys.Zip(dark)))
            values[key.Key] = colour;
        return new PlacementState(CustomTheme.Surface.Key, CustomTheme.Surface.DefaultOrder + order * 10, false, Audience.Everyone.Text, null, null, values);
    }
}
