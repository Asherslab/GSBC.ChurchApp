namespace GSBC.ChurchApp.Platform.Api.Media;

public interface IMediaHolder
{
    bool Holds(string owner);

    Task RefreshAsync(string owner, CancellationToken cancellationToken);
}
