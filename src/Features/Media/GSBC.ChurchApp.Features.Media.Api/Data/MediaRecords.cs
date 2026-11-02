namespace GSBC.ChurchApp.Features.Media.Api.Data;

public sealed class MediaItem
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string ContentType { get; set; }
    public long Bytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string FileName { get; set; } = "";
    public string AltText { get; set; } = "";
    public string? AudiencePermission { get; set; }
    public string? AudienceScopeId { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }

    public bool IsPrivate => AudiencePermission is not null;
}

public sealed class PendingUpload
{
    public Guid Id { get; set; }
    public Guid UploadedBy { get; set; }
    public required string ContentType { get; set; }
    public string FileName { get; set; } = "";
    public string AltText { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public string? AudiencePermission { get; set; }
    public string? AudienceScopeId { get; set; }
    public DateTime ExpiresAt { get; set; }

    public string StagingKey => $"uploads/{Id:N}";
}
