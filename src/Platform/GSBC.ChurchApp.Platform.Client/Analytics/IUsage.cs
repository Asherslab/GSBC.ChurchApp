using GSBC.ChurchApp.Platform.Contracts.Analytics;

namespace GSBC.ChurchApp.Platform.Client.Analytics;

public interface IUsage
{
    void Count(Measure measure, string? subject = null);
}

internal sealed class NoUsage : IUsage
{
    public void Count(Measure measure, string? subject = null)
    {
    }
}
