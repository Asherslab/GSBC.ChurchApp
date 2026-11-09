namespace GSBC.ChurchApp.Platform.Ui.Tabs;

public interface ILiveTabIcon
{
    string Route { get; }

    string Icon { get; }

    event Action? Changed;
}
