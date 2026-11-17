using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Locations;

public enum FoundLocationKind
{
    Address = 0,
    Suburb = 1,
    Named = 2,
}

[ProtoContract]
public sealed class LocationSearch
{
    [ProtoMember(1)] public string Text { get; set; } = "";
}

[ProtoContract]
public sealed class LocationPoint
{
    [ProtoMember(1)] public double Latitude { get; set; }
    [ProtoMember(2)] public double Longitude { get; set; }
}

[ProtoContract]
public sealed class FoundLocations
{
    [ProtoMember(1)] public List<FoundLocation> Found { get; set; } = [];
    [ProtoMember(2)] public bool Indexed { get; set; }
}

[ProtoContract]
public sealed class FoundLocation
{
    [ProtoMember(1)] public FoundLocationKind Kind { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Address { get; set; } = "";
    [ProtoMember(4)] public string Suburb { get; set; } = "";
    [ProtoMember(5)] public double Latitude { get; set; }
    [ProtoMember(6)] public double Longitude { get; set; }
    [ProtoMember(7)] public double FromChurchKm { get; set; }
    [ProtoMember(8)] public string Direction { get; set; } = "";
}
