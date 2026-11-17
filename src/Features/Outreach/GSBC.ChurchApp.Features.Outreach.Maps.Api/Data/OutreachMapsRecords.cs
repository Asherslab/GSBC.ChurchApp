namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

public sealed class MapAreaRecord
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public double South { get; set; }
    public double West { get; set; }
    public double North { get; set; }
    public double East { get; set; }
    public required string State { get; set; }
    public string? Stage { get; set; }
    public string? Problem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? BuiltAt { get; set; }
    public string? Report { get; set; }
    public double CentreLatitude { get; set; }
    public double CentreLongitude { get; set; }
    public double CellLatitude { get; set; }
    public double CellLongitude { get; set; }
    public int OriginRow { get; set; }
    public int OriginColumn { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int StepsDone { get; set; }
    public int Steps { get; set; }
    public string GridId { get; set; } = "";
}

public sealed class MapCellRecord
{
    public required string AreaId { get; set; }
    public required string SquareId { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public bool On { get; set; }
}

public sealed class MapSquareRecord
{
    public required string Id { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public bool Pending { get; set; }
    public bool Empty { get; set; }
    public long Version { get; set; }
    public string Name { get; set; } = "";
}

public sealed class PlaceRecord
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Kind { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Population { get; set; }
    public DateTime IndexedAt { get; set; }
}

public sealed class AddressRecord : IAddressRow
{
    public long Id { get; set; }
    public required string RegionId { get; set; }
    public required string Number { get; set; }
    public required string Unit { get; set; }
    public required string Street { get; set; }
    public required string Locality { get; set; }
    public required string Key { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class RegionRecord
{
    public required string Id { get; set; }
    public required string GridId { get; set; }
    public required string Name { get; set; }
    public double CoreSouth { get; set; }
    public double CoreWest { get; set; }
    public double CoreNorth { get; set; }
    public double CoreEast { get; set; }
    public double South { get; set; }
    public double West { get; set; }
    public double North { get; set; }
    public double East { get; set; }
    public long Version { get; set; }
    public DateTime BuiltAt { get; set; }
    public required string StreetsKey { get; set; }
    public required string StreetsFingerprint { get; set; }
    public long StreetsBytes { get; set; }
    public required string MapKey { get; set; }
    public long MapBytes { get; set; }
    public int MapMaxZoom { get; set; }
    public int Letterboxes { get; set; }
}

public sealed class RegionFileRecord
{
    public required string Key { get; set; }
    public required string RegionId { get; set; }
    public long Version { get; set; }
}

public sealed class OverviewRecord
{
    public required string Id { get; set; }
    public required string MapKey { get; set; }
    public long MapBytes { get; set; }
    public int MaxZoom { get; set; }
    public DateTime BuiltAt { get; set; }
}

public sealed class NationalAddressRecord : IAddressRow
{
    public required string Id { get; set; }
    public int Release { get; set; }
    public required string Number { get; set; }
    public required string Unit { get; set; }
    public required string Street { get; set; }
    public required string Locality { get; set; }
    public required string State { get; set; }
    public required string Postcode { get; set; }
    public required string Building { get; set; }
    public required string BuildingKey { get; set; }
    public required string Key { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class NationalLocalityRecord
{
    public required string Id { get; set; }
    public int Release { get; set; }
    public required string Name { get; set; }
    public required string State { get; set; }
    public required string Key { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class NationalReleaseRecord
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Source { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public long Addresses { get; set; }
    public long Localities { get; set; }
}

public sealed class PlaceMapRecord
{
    public required string Id { get; set; }
    public required string MapKey { get; set; }
    public long MapBytes { get; set; }
    public int MaxZoom { get; set; }
    public double South { get; set; }
    public double West { get; set; }
    public double North { get; set; }
    public double East { get; set; }
    public DateTime BuiltAt { get; set; }
}

