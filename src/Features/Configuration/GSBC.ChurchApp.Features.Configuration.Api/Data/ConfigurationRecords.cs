namespace GSBC.ChurchApp.Features.Configuration.Api.Data;

public sealed class PlacementRecord
{
    public Guid Id { get; set; }
    public required string Area { get; set; }
    public string? ScopeId { get; set; }
    public required string Surface { get; set; }
    public bool IsSingleton { get; set; }
    public int Order { get; set; }
    public bool Hidden { get; set; }
    public string Audience { get; set; } = "";
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public Dictionary<string, string> Values { get; set; } = [];
    public long Revision { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
}

public sealed class ChangeRecord
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public required string Area { get; set; }
    public string? ScopeId { get; set; }
    public Guid PlacementId { get; set; }
    public required string Surface { get; set; }
    public string? Before { get; set; }
    public string? After { get; set; }
    public DateTime At { get; set; }
    public Guid By { get; set; }
    public Guid? RollsBack { get; set; }
}
