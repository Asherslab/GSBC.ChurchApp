namespace GSBC.ChurchApp.Platform.Data.Email;

public sealed class OutgoingEmail
{
    public long Id { get; set; }
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? GaveUpAt { get; set; }
    public string? LastError { get; set; }
}
