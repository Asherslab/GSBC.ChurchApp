using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Sermons.Api.Data;

public sealed class SermonRecord
{
    public Guid Id { get; set; }
    public required string Origin { get; set; }
    public string? ExternalId { get; set; }
    public string? WebLink { get; set; }
    public DateTime PreachedAt { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string SourceTitle { get; set; } = "";
    public string SourceSpeaker { get; set; } = "";
    public Guid? SourceSpeakerId { get; set; }
    public Guid? SourceSeriesId { get; set; }
    public string SourceService { get; set; } = "";
    public string? SourceYouTubeId { get; set; }
    public string Summary { get; set; } = "";

    public string? Title { get; set; }
    public Guid? SpeakerId { get; set; }
    public bool SeriesCorrected { get; set; }
    public Guid? SeriesId { get; set; }
    public string? Service { get; set; }
    public bool YouTubeCorrected { get; set; }
    public string? YouTubeId { get; set; }
    public bool Hidden { get; set; }

    public string AudioUrl { get; set; } = "";
    public string AudioType { get; set; } = "audio/mpeg";
    public long AudioBytes { get; set; }
    public int DurationSeconds { get; set; }
    public string? AudioCopiedFrom { get; set; }
    public DateTime? CopyStartedAt { get; set; }
    public long CopiedBytes { get; set; }
    public string? CopyProblem { get; set; }
    public MediaRef? AudioMedia { get; set; }

    public string? FeedArtworkUrl { get; set; }
    public DateTime? PageReadAt { get; set; }
    public string? ArtworkSourceUrl { get; set; }
    public string? ArtworkHash { get; set; }
    public MediaRef? Artwork { get; set; }

    public string? PublishedHash { get; set; }
    public DateTime? AnnouncedAt { get; set; }
    public DateTime? CheckedAt { get; set; }
    public Guid? CheckedBy { get; set; }

    public string EffectiveTitle => Title ?? SourceTitle;
    public Guid? EffectiveSpeakerId => SpeakerId ?? SourceSpeakerId;
    public Guid? EffectiveSeriesId => SeriesCorrected ? SeriesId : SourceSeriesId;
    public string EffectiveService => Service ?? SourceService;
    public string? EffectiveYouTubeId => YouTubeCorrected ? YouTubeId : SourceYouTubeId;
}

public sealed class SeriesRecord
{
    public Guid Id { get; set; }
    public string? Slug { get; set; }
    public required string Name { get; set; }
    public string? SourceName { get; set; }
    public string Description { get; set; } = "";
    public string? ArtworkSourceUrl { get; set; }
    public string? ArtworkHash { get; set; }
    public MediaRef? Artwork { get; set; }
    public MediaRef? ChosenArtwork { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PublishedHash { get; set; }
}

public sealed class SpeakerRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? PhotoSourceUrl { get; set; }
    public string? PhotoHash { get; set; }
    public MediaRef? Photo { get; set; }
    public MediaRef? ChosenPhoto { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PublishedHash { get; set; }
}

public sealed class SpeakerAlias
{
    public required string Alias { get; set; }
    public Guid SpeakerId { get; set; }
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

public sealed class SourceState
{
    public const int SingleId = 1;

    public int Id { get; set; } = SingleId;
    public string Source { get; set; } = Contracts.SermonSources.Website;
    public string? ETag { get; set; }
    public string? LastModified { get; set; }
    public DateTime? CheckedAt { get; set; }
    public DateTime? ChangedAt { get; set; }
    public string? Problem { get; set; }
    public DateTime? SeriesCheckedAt { get; set; }
    public string? SeriesProblem { get; set; }
    public bool HasReadOnce { get; set; }
}

public sealed class FormerId
{
    public const string Sermon = "sermon";
    public const string Series = "series";
    public const string Speaker = "speaker";

    public Guid Id { get; set; }
    public Guid CurrentId { get; set; }
    public required string Kind { get; set; }
    public DateTime MovedAt { get; set; }
    public bool Settled { get; set; }
}

public sealed class ArtworkSource
{
    public required string Hash { get; set; }
    public MediaRef? Picture { get; set; }
    public DateTime AddedAt { get; set; }
}

public sealed class ArtworkAddress
{
    public required string Url { get; set; }
    public required string Hash { get; set; }
    public string? ETag { get; set; }
    public string? LastModified { get; set; }
    public DateTime FetchedAt { get; set; }
}
