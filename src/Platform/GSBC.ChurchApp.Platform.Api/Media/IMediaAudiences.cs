namespace GSBC.ChurchApp.Platform.Api.Media;

public interface IMediaAudiences
{
    Task<bool> ShareAsync(MediaSystemArea area, string mediaId, string owner, string? permissionNode, string? scopeId, CancellationToken cancellationToken);
}
