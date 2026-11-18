using GSBC.ChurchApp.Features.Outreach.Maps.Api.Addresses;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.LandUse;

internal sealed class LandUseMap
{
    public const string MoretonBayLotPlan = "LOT_PLAN";
    public const string MoretonBayLandUse = "LanduseRates";
    public const string BrisbaneZoneCode = "ZONE_CODE";
    public const string BrisbanePrecinct = "ZONE_PREC_DESC";

    private readonly Dictionary<string, Parcel> _moretonBayByLot = [];
    private readonly PolygonIndex _moretonBayLots;
    private readonly PolygonIndex _brisbaneZones;

    public LandUseMap(IReadOnlyList<Parcel> moretonBayLots, IReadOnlyList<Parcel> brisbaneZones)
    {
        foreach (Parcel lot in moretonBayLots)
            if (lot.Attribute(MoretonBayLotPlan).Trim() is { Length: > 0 } key)
                _moretonBayByLot[key] = lot;
        _moretonBayLots = new PolygonIndex(moretonBayLots);
        _brisbaneZones = new PolygonIndex(brisbaneZones);
    }

    public LandUseVerdict Classify(AddressPoint address)
    {
        if (address.Council.StartsWith("Brisbane", StringComparison.OrdinalIgnoreCase))
            return _brisbaneZones.Find(address.Point) is { } zone
                ? LandUseRules.Brisbane(zone.Attribute(BrisbaneZoneCode), zone.Attribute(BrisbanePrecinct))
                : LandUseVerdict.CheckFor("no zone");
        if (address.Council.StartsWith("Moreton Bay", StringComparison.OrdinalIgnoreCase))
            return (_moretonBayByLot.GetValueOrDefault(address.LotPlan.Trim()) ?? _moretonBayLots.Find(address.Point)) is { } lot
                ? LandUseRules.MoretonBay(lot.Attribute(MoretonBayLandUse))
                : LandUseVerdict.CheckFor("no land use record");
        return LandUseVerdict.CheckFor("no land use data for this council");
    }
}
