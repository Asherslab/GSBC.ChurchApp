namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class ReviewCode
{
    public Guid Id { get; set; }
    public required string Hash { get; set; }
    public Guid UserId { get; set; }
    public required string Label { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? TurnedOffAt { get; set; }
    public int Uses { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public bool IsLive(DateTime now) => TurnedOffAt is null && ExpiresAt > now;
}
