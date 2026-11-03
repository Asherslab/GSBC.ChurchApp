namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AppInstall
{
    public Guid Id { get; set; }
    public required string KeyId { get; set; }
    public required byte[] PublicKey { get; set; }
    public long Counter { get; set; }
    public required string AppId { get; set; }
    public AttestEnvironment Environment { get; set; }
    public DateTime AttestedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool Blocked { get; set; }
}

public enum AttestEnvironment
{
    Development,
    Production,
}

public sealed class InstallChallenge
{
    public required byte[] Challenge { get; set; }
    public DateTime ExpiresAt { get; set; }
}
