using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Platform.Api.Analytics;

public sealed class MeasureCatalog(IEnumerable<MeasureSet> sets)
{
    public IReadOnlyDictionary<string, Measure> ByKey { get; } =
        sets.SelectMany(set => set.Measures).ToDictionary(measure => measure.Key);
}

public sealed record MeasureSet(IReadOnlyList<Measure> Measures);
