namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class OneTimeCode
{
    public required string Hash { get; set; }
    public Guid UserId { get; set; }
    public CodePurpose Purpose { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public int FailedAttempts { get; set; }
}

public enum CodePurpose
{
    ConfirmEmail,
    Invitation,
    Recovery,
    RegisterPasskey,
}
