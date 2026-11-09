namespace GSBC.ChurchApp.Features.People.Api.Data;

public sealed class PersonRecord
{
    public Guid UserId { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool SaysAdult { get; set; }
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
}
