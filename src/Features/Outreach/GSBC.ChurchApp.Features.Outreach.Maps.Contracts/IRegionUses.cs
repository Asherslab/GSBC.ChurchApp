namespace GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

public interface IRegionUses
{
    Task<IReadOnlyList<RegionUse>> UsesAsync(IReadOnlyCollection<string> regionIds, CancellationToken cancellationToken);
}
