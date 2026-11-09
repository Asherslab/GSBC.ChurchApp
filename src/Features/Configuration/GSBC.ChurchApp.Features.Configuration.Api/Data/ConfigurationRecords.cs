using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

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
    public Guid? DraftId { get; set; }
    public string? DraftName { get; set; }
}

public sealed class DraftRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DraftStatus Status { get; set; }
    public DateTime? AskedAt { get; set; }
    public bool LayerOn { get; set; }
    public DateTime? LayerStartsAt { get; set; }
    public DateTime? LayerEndsAt { get; set; }
    public List<string> DroppedFromLayer { get; set; } = [];
    public DateTime? PublishAt { get; set; }
    public Guid? PublishScheduledBy { get; set; }
    public string? NotPublished { get; set; }
    public long Revision { get; set; }
}

public sealed class DraftChangeRecord
{
    public Guid Id { get; set; }
    public Guid DraftId { get; set; }
    public required string Area { get; set; }
    public string? ScopeId { get; set; }
    public Guid PlacementId { get; set; }
    public required string Surface { get; set; }
    public PatchKind Kind { get; set; }
    public long BasedOnRevision { get; set; }
    public required string Patch { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid ChangedBy { get; set; }
}

public sealed class SeedRecord
{
    public required string Key { get; set; }
    public DateTime SeededAt { get; set; }
}
