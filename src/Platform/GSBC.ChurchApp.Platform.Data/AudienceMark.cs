namespace GSBC.ChurchApp.Platform.Data;

public sealed class AudienceMark
{
    public required string SessionId { get; set; }
    public required string Feed { get; set; }
    public required string Fingerprint { get; set; }
    public DateTime SeenAt { get; set; }
}
