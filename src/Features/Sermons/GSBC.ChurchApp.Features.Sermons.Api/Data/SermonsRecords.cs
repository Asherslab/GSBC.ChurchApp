using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Sermons.Api.Data;

public sealed class SermonRecord
{
    public Guid Id { get; set; }
    public DateTime PreachedAt { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string Title { get; set; } = "";
    public Guid? SpeakerId { get; set; }
    public Guid? SeriesId { get; set; }
    public string Service { get; set; } = "";
    public string? YouTubeId { get; set; }
    public string Passage { get; set; } = "";
    public string Summary { get; set; } = "";
    public bool Hidden { get; set; }

    public string AudioUrl { get; set; } = "";
    public string AudioType { get; set; } = "audio/mpeg";
    public long AudioBytes { get; set; }
    public int DurationSeconds { get; set; }
    public MediaRef? AudioMedia { get; set; }
    public MediaRef? Artwork { get; set; }

    public string? PublishedHash { get; set; }
    public DateTime? AnnouncedAt { get; set; }
    public DateTime? CheckedAt { get; set; }
    public Guid? CheckedBy { get; set; }
}

public sealed class SeriesRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public MediaRef? Artwork { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PublishedHash { get; set; }
}

public sealed class SpeakerRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Honorific { get; set; }
    public MediaRef? Photo { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PublishedHash { get; set; }
}

public sealed class ProgressRecord
{
    public Guid UserId { get; set; }
    public Guid SermonId { get; set; }
    public double Position { get; set; }
    public double Duration { get; set; }
    public DateTime? HeardAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime? WatchedAt { get; set; }
    public DateTime? ClearedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
