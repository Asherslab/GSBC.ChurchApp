namespace GSBC.ChurchApp.Platform.Client.Notifications;

public interface IFollowedScopes
{
    IReadOnlySet<string> Scopes { get; }
}
