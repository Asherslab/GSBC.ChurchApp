namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AuditEntry
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ActorId { get; set; }
    public required string Action { get; set; }
    public string? Detail { get; set; }
    public string? RemoteAddress { get; set; }
}
