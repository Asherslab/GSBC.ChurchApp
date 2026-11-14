using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Ui.Tabs;

public interface ITabBarNow
{
    Task<IReadOnlyList<AppTab>> CurrentAsync();
}
