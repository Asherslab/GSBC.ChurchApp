namespace GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

public interface IAddressSearch
{
    Task<IReadOnlyList<FoundAddress>> FindAsync(string text, CancellationToken cancellationToken);
}

public sealed record FoundAddress(uint AddressKey, string Label, string RegionId, double Latitude, double Longitude);
