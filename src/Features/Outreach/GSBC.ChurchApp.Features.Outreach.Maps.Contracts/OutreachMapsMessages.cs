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
