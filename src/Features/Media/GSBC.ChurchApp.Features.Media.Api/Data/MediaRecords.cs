using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Media.Api.Data;

public sealed class MediaItem
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Sha256 { get; set; }
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
    public Guid? FolderId { get; set; }
    public string? Area { get; set; }
    public PictureSizing Sizing { get; set; }
    public DateTime? OriginalRemovedAt { get; set; }

    public bool IsPrivate => AudiencePermission is not null;

    public bool KeepsOriginal => OriginalRemovedAt is null;
}

public sealed class MediaRendition
{
    public Guid MediaId { get; set; }
    public MediaSizeName Size { get; set; }
    public required string Key { get; set; }
    public required string Sha256 { get; set; }
    public required string ContentType { get; set; }
    public long Bytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class MediaAlias
{
    public Guid Id { get; set; }
    public Guid MediaId { get; set; }
}

public sealed class MediaFolder
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public required string Name { get; set; }
    public string? SystemArea { get; set; }
    public string? SystemScope { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsSystem => SystemArea is not null;
}

public sealed class PendingUpload
{
    public Guid Id { get; set; }
    public Guid UploadedBy { get; set; }
    public required string ContentType { get; set; }
    public string FileName { get; set; } = "";
    public string AltText { get; set; } = "";
    public string? AudiencePermission { get; set; }
    public string? AudienceScopeId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid? FolderId { get; set; }
    public string? Area { get; set; }

    public string StagingKey => $"uploads/{Id:N}";
}

public sealed class MediaUse
{
    public Guid MediaId { get; set; }
    public required string Owner { get; set; }
}
