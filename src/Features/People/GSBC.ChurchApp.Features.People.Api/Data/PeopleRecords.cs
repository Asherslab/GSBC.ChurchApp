namespace GSBC.ChurchApp.Features.People.Api.Data;

public sealed class PersonRecord
{
    public Guid UserId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool SaysAdult { get; set; }
    public string? Reach { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class FamilyLink
{
    public Guid ChildId { get; set; }
    public Guid ParentId { get; set; }
    public required string How { get; set; }
    public DateTime LinkedAt { get; set; }
    public Guid? LinkedBy { get; set; }
}

public sealed class LinkAskRecord
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public required string ParentEmail { get; set; }
    public Guid AskedBy { get; set; }
    public DateTime AskedAt { get; set; }
}

public sealed class SafeguardingEntry
{
    public Guid UserId { get; set; }
    public string? CardNumber { get; set; }
    public string? CardStatus { get; set; }
    public DateOnly? CardExpires { get; set; }
    public DateOnly? CardChecked { get; set; }
    public DateOnly? TrainingDone { get; set; }
    public DateOnly? TrainingDue { get; set; }
    public required string Source { get; set; }
    public string? ExternalId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public string? LinkedHow { get; set; }
    public DateTime? LinkedAt { get; set; }
    public Guid? LinkedBy { get; set; }
    public DateTime? SyncedAt { get; set; }
    public string? TrainingProgress { get; set; }
    public string? OtherTrainingName { get; set; }
    public DateOnly? OtherTrainingDone { get; set; }
    public string? SmcStatus { get; set; }
    public string? SmcStatusWords { get; set; }
    public List<string> CheckReasons { get; set; } = [];
}

public sealed class SmcPersonRecord
{
    public required string SmcId { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool LinkByHandOnly { get; set; }
    public string? Why { get; set; }
    public DateTime SeenAt { get; set; }
}

public sealed class SmcSyncState
{
    public const int Only = 1;

    public int Id { get; set; } = Only;
    public DateTime? LastRunAt { get; set; }
    public DateTime? LastSucceededAt { get; set; }
    public string? Problem { get; set; }
    public int People { get; set; }
    public int Linked { get; set; }
    public int ToMatch { get; set; }
    public int ToCheck { get; set; }
}
