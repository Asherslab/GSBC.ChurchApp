using System.ComponentModel;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

public static class MapAreaStates
{
    public const string Waiting = "waiting";
    public const string Building = "building";
    public const string Built = "built";
    public const string Failed = "failed";
}

[ProtoContract]
public sealed class MapBounds
{
    [ProtoMember(1)] public double South { get; set; }
    [ProtoMember(2)] public double West { get; set; }
    [ProtoMember(3)] public double North { get; set; }
    [ProtoMember(4)] public double East { get; set; }
}

[ProtoContract]
public sealed class MapFile
{
    [ProtoMember(1)] public string Url { get; set; } = "";
    [ProtoMember(2)] public long Bytes { get; set; }
}

[ProtoContract]
public sealed class OutreachRegion
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string AreaId { get; set; } = "";
    [ProtoMember(3)] public string Name { get; set; } = "";
    [ProtoMember(4, IsRequired = true)] public MapBounds Core { get; set; } = new();
    [ProtoMember(5, IsRequired = true)] public MapBounds Bounds { get; set; } = new();
    [ProtoMember(6)] public long Version { get; set; }
    [ProtoMember(7)] public DateTime BuiltAt { get; set; }
    [ProtoMember(8, IsRequired = true)] public MapFile Streets { get; set; } = new();
    [ProtoMember(9, IsRequired = true)] public MapFile Map { get; set; } = new();
    [ProtoMember(10)] public int MapMaxZoom { get; set; }
    [ProtoMember(11)] public int Letterboxes { get; set; }
    [ProtoMember(12)] public string Attribution { get; set; } = "";
}

[ProtoContract]
public sealed class MapArea
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3, IsRequired = true)] public MapBounds Bounds { get; set; } = new();
    [ProtoMember(4), DefaultValue(MapAreaStates.Waiting)] public string State { get; set; } = MapAreaStates.Waiting;
    [ProtoMember(5)] public string? Stage { get; set; }
    [ProtoMember(6)] public string? Problem { get; set; }
    [ProtoMember(7)] public DateTime? StartedAt { get; set; }
    [ProtoMember(8)] public DateTime? BuiltAt { get; set; }
    [ProtoMember(9)] public List<OutreachRegion> Regions { get; set; } = [];
    [ProtoMember(10)] public MapAreaReport? Report { get; set; }
    [ProtoMember(11, IsRequired = true)] public MapGrid Grid { get; set; } = new();
    [ProtoMember(12)] public List<MapCell> Cells { get; set; } = [];
    [ProtoMember(13)] public int CellLimit { get; set; }
}

[ProtoContract]
public sealed class MapCell
{
    [ProtoMember(1)] public int Row { get; set; }
    [ProtoMember(2)] public int Column { get; set; }
    [ProtoMember(3)] public string RegionId { get; set; } = "";
    [ProtoMember(4)] public bool On { get; set; }
    [ProtoMember(5)] public bool Pending { get; set; }
    [ProtoMember(6)] public bool Empty { get; set; }

    public CellRef Ref => new() { Row = Row, Column = Column };
}

[ProtoContract]
public sealed class MapAreaReport
{
    [ProtoMember(1)] public int Streets { get; set; }
    [ProtoMember(2)] public int Addresses { get; set; }
    [ProtoMember(3)] public int Placed { get; set; }
    [ProtoMember(4)] public int Homes { get; set; }
    [ProtoMember(5)] public int Checks { get; set; }
    [ProtoMember(6)] public int Skipped { get; set; }
    [ProtoMember(7)] public int FootpathsBesideRoads { get; set; }
    [ProtoMember(8)] public long StreetsBytes { get; set; }
    [ProtoMember(9)] public long MapBytes { get; set; }
    [ProtoMember(10)] public int Seconds { get; set; }
}

[ProtoContract]
public sealed class MapAreaList
{
    [ProtoMember(1)] public List<MapArea> Areas { get; set; } = [];
}

[ProtoContract]
public sealed class MapAreaListRequest;

[ProtoContract]
public sealed class RegionList
{
    [ProtoMember(1)] public List<OutreachRegion> Regions { get; set; } = [];
}

[ProtoContract]
public sealed class RegionListRequest;

[ProtoContract]
public sealed class AddMapArea
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public double CentreLatitude { get; set; }
    [ProtoMember(3)] public double CentreLongitude { get; set; }
    [ProtoMember(4)] public double RadiusKm { get; set; }
}

[ProtoContract]
public sealed class MapAreaRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class RenameMapArea
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class PlaceSearch
{
    [ProtoMember(1)] public string Text { get; set; } = "";
}

public static class MapPlaceKinds
{
    public const string City = "city";
    public const string Town = "town";
    public const string Suburb = "suburb";
    public const string Village = "village";
    public const string Locality = "locality";
    public const string Hamlet = "hamlet";
    public const string Neighbourhood = "neighbourhood";
}

[ProtoContract]
public sealed class MapPlace
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public string Kind { get; set; } = "";
    [ProtoMember(3)] public double Latitude { get; set; }
    [ProtoMember(4)] public double Longitude { get; set; }
    [ProtoMember(5)] public double FromChurchKm { get; set; }
    [ProtoMember(6)] public string Direction { get; set; } = "";
}

[ProtoContract]
public sealed class PlaceList
{
    [ProtoMember(1)] public List<MapPlace> Places { get; set; } = [];
    [ProtoMember(2)] public bool Indexed { get; set; }
}

[ProtoContract]
public sealed class MapAreaPlan
{
    [ProtoMember(1)] public double CentreLatitude { get; set; }
    [ProtoMember(2)] public double CentreLongitude { get; set; }
    [ProtoMember(3)] public double RadiusKm { get; set; }
}

[ProtoContract]
public sealed class CellEstimate
{
    [ProtoMember(1)] public int Row { get; set; }
    [ProtoMember(2)] public int Column { get; set; }
    [ProtoMember(3)] public int Addresses { get; set; }
    [ProtoMember(4)] public long Bytes { get; set; }

    public bool HasHomes => Addresses > 0;
}

[ProtoContract]
public sealed class MapAreaPreview
{
    [ProtoMember(1, IsRequired = true)] public MapGrid Grid { get; set; } = new();
    [ProtoMember(2)] public List<CellEstimate> Cells { get; set; } = [];
    [ProtoMember(3)] public List<MapPlace> Places { get; set; } = [];
}

[ProtoContract]
public sealed class RegionChange
{
    [ProtoMember(1)] public string AreaId { get; set; } = "";
    [ProtoMember(2)] public List<CellRef> Off { get; set; } = [];
    [ProtoMember(3)] public List<CellRef> On { get; set; } = [];
}

public static class RegionUseKinds
{
    public const string Day = "day";
    public const string Campaign = "campaign";
}

[ProtoContract]
public sealed class RegionUse
{
    [ProtoMember(1)] public string RegionId { get; set; } = "";
    [ProtoMember(2)] public string Kind { get; set; } = "";
    [ProtoMember(3)] public string Name { get; set; } = "";
    [ProtoMember(4)] public DateTime? On { get; set; }
}

[ProtoContract]
public sealed class RegionChangeCheck
{
    [ProtoMember(1)] public List<CellEstimate> Added { get; set; } = [];
    [ProtoMember(2)] public List<RegionUse> Uses { get; set; } = [];
    [ProtoMember(3)] public long FreedBytes { get; set; }
    [ProtoMember(4)] public string? Problem { get; set; }
}
