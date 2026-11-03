using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Platform.Client.Analytics;

public interface IWeeklyState
{
    Measure Measure { get; }

    ValueTask<string> CurrentAsync();
}
