namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

public interface IAddressRow
{
    string Number { get; }
    string Unit { get; }
    string Street { get; }
    string Locality { get; }
    string Key { get; }
}
