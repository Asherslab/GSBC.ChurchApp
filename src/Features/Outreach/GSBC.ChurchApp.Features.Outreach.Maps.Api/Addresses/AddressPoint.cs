using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;

internal sealed record AddressPoint(uint Id, GeoPoint Point, string Street, bool IsUnit, string LotPlan, string Locality, string Council);
