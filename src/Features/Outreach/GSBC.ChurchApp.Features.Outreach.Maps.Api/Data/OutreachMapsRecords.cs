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
}

public sealed class RegionRecord
{
    public required string Id { get; set; }
    public required string AreaId { get; set; }
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
