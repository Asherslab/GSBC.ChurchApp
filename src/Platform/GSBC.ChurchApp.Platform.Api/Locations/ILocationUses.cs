namespace GSBC.ChurchApp.Platform.Api.Locations;

public interface ILocationUses
{
    Task<IReadOnlyCollection<string>> MapKeysAsync(CancellationToken cancellationToken);
}
