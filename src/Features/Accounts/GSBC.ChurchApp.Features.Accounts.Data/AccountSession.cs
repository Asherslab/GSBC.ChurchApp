namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AccountSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public SessionClient Client { get; set; }
    public string? DeviceName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public byte[] Ticket { get; set; } = [];
    public string? SecretHash { get; set; }
    public Guid? ReviewCodeId { get; set; }
}

public enum SessionClient
{
    Browser,
    App,
    Tool,
}
