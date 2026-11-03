namespace GSBC.ChurchApp.Platform.Client.Notifications;

public interface IInboxCount
{
    int Unread { get; }

    string Href { get; }

    event Action? Changed;
}
