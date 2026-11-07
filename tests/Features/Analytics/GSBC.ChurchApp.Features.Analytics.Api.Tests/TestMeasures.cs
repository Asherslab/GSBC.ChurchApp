using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

internal static class TestMeasures
{
    public static readonly Measure TileTapped =
        new("hub.tile.tapped", "Home screen tiles tapped", "Taps on each tile", SubjectKinds.HubTile, SplitByGroup: true);

    public static readonly Measure Notifications =
        new("notifications.push", "Notifications", "Phones by whether notifications are on", OncePerPhone: true,
            States: [new("on", "On"), new("off", "Turned off")]);

    public static MeasureCatalog Catalog => new([new MeasureSet([.. AnalyticsMeasures.All, TileTapped, Notifications])]);
}
