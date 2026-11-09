using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class Sermon
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public DateTime PreachedAt { get; set; }
    [ProtoMember(4)] public string Service { get; set; } = "";
    [ProtoMember(5)] public string SpeakerId { get; set; } = "";
    [ProtoMember(6)] public string SpeakerName { get; set; } = "";
    [ProtoMember(7)] public string? SeriesId { get; set; }
    [ProtoMember(8)] public string? SeriesName { get; set; }
    [ProtoMember(9)] public int Part { get; set; }
    [ProtoMember(10)] public string AudioUrl { get; set; } = "";
    [ProtoMember(11)] public string AudioType { get; set; } = "";
    [ProtoMember(12)] public long AudioBytes { get; set; }
    [ProtoMember(13)] public int DurationSeconds { get; set; }
    [ProtoMember(14)] public string? YouTubeId { get; set; }
    [ProtoMember(15)] public MediaRef? Artwork { get; set; }
    [ProtoMember(17)] public string Summary { get; set; } = "";
    [ProtoMember(20)] public string Passage { get; set; } = "";
}

[ProtoContract]
public sealed class SeriesInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Description { get; set; } = "";
    [ProtoMember(4)] public MediaRef? Artwork { get; set; }
    [ProtoMember(6)] public int Count { get; set; }
    [ProtoMember(7)] public DateTime FirstAt { get; set; }
    [ProtoMember(8)] public DateTime LastAt { get; set; }
}

[ProtoContract]
public sealed class SpeakerInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public MediaRef? Photo { get; set; }
    [ProtoMember(4)] public int Count { get; set; }
    [ProtoMember(5)] public DateTime LatestAt { get; set; }
}

[ProtoContract]
public sealed class ListeningEntry
{
    [ProtoMember(1)] public string SermonId { get; set; } = "";
    [ProtoMember(2)] public double Position { get; set; }
    [ProtoMember(3)] public double Duration { get; set; }
    [ProtoMember(4)] public DateTime? HeardAt { get; set; }
    [ProtoMember(5)] public DateTime? FinishedAt { get; set; }
    [ProtoMember(6)] public DateTime? WatchedAt { get; set; }
    [ProtoMember(7)] public DateTime? ClearedAt { get; set; }
}

[ProtoContract]
public sealed class ProgressBatch
{
    [ProtoMember(1)] public List<ListeningEntry> Entries { get; set; } = [];
}

[ProtoContract]
public sealed class LatestQuery
{
    [ProtoMember(1)] public int Count { get; set; }
}

[ProtoContract]
public sealed class SermonPage
{
    [ProtoMember(1)] public List<Sermon> Sermons { get; set; } = [];
}

[ProtoContract]
public sealed class FollowRequest
{
    [ProtoMember(1)] public string? SeriesId { get; set; }
    [ProtoMember(2)] public bool Follow { get; set; }
}

[ProtoContract]
public sealed class Follows
{
    [ProtoMember(1)] public bool NewSermons { get; set; }
    [ProtoMember(2)] public List<string> SeriesIds { get; set; } = [];
}

[ProtoContract]
public sealed class SermonQuery
{
    [ProtoMember(1)] public string Text { get; set; } = "";
    [ProtoMember(2)] public string Filter { get; set; } = "";
    [ProtoMember(3)] public int Skip { get; set; }
    [ProtoMember(4)] public int Take { get; set; }
}

[ProtoContract]
public sealed class SermonList
{
    [ProtoMember(1)] public List<SermonDetail> Sermons { get; set; } = [];
    [ProtoMember(2)] public int Total { get; set; }
    [ProtoMember(3)] public Dictionary<string, int> Counts { get; set; } = [];
    [ProtoMember(4)] public string? PodcastFeedUrl { get; set; }
}

[ProtoContract]
public sealed class SermonDetail
{
    [ProtoMember(1, IsRequired = true)] public Sermon Sermon { get; set; } = new();
    [ProtoMember(3)] public bool Hidden { get; set; }
    [ProtoMember(6)] public string? SuggestedSeries { get; set; }
    [ProtoMember(7)] public DateTime AddedAt { get; set; }
    [ProtoMember(8)] public DateTime? CheckedAt { get; set; }
    [ProtoMember(9)] public List<string> NeedsALook { get; set; } = [];
}

[ProtoContract]
public sealed class SermonCorrection
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string? Title { get; set; }
    [ProtoMember(3)] public string? SpeakerId { get; set; }
    [ProtoMember(4)] public string? SeriesId { get; set; }
    [ProtoMember(5)] public string? Service { get; set; }
    [ProtoMember(6)] public string? YouTube { get; set; }
    [ProtoMember(7)] public bool? Hidden { get; set; }
    [ProtoMember(9)] public bool? Checked { get; set; }
    [ProtoMember(10)] public string? Passage { get; set; }
    [ProtoMember(11)] public string? Summary { get; set; }
}

[ProtoContract]
public sealed class SeriesDetail
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Description { get; set; } = "";
    [ProtoMember(4)] public MediaRef? Artwork { get; set; }
    [ProtoMember(5)] public int Count { get; set; }
    [ProtoMember(6)] public DateTime? LastAt { get; set; }
}

[ProtoContract]
public sealed class SeriesList
{
    [ProtoMember(1)] public List<SeriesDetail> Series { get; set; } = [];
}

[ProtoContract]
public sealed class SpeakerDetail
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public MediaRef? Photo { get; set; }
    [ProtoMember(5)] public int Count { get; set; }
}

[ProtoContract]
public sealed class SpeakerList
{
    [ProtoMember(1)] public List<SpeakerDetail> Speakers { get; set; } = [];
}

[ProtoContract]
public sealed class DeleteRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string? MoveTo { get; set; }
}

[ProtoContract]
public sealed class SeriesDeletion
{
    [ProtoMember(1)] public int Sermons { get; set; }
    [ProtoMember(2)] public int Followers { get; set; }
}

[ProtoContract]
public sealed class ServiceChoice
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public bool Standard { get; set; }
    [ProtoMember(3)] public int Count { get; set; }
}

[ProtoContract]
public sealed class ServiceList
{
    [ProtoMember(1)] public List<ServiceChoice> Services { get; set; } = [];
}

[ProtoContract]
public sealed class AudioUploadRequest
{
    [ProtoMember(1)] public string FileName { get; set; } = "";
    [ProtoMember(2)] public string ContentType { get; set; } = "";
    [ProtoMember(3)] public long Bytes { get; set; }
}

[ProtoContract]
public sealed class AudioUploadTicket
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public string Url { get; set; } = "";
    [ProtoMember(3)] public string ContentType { get; set; } = "";
    [ProtoMember(4)] public DateTime ExpiresAt { get; set; }
}

[ProtoContract]
public sealed class SermonUpload
{
    [ProtoMember(1)] public string AudioKey { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public string? SpeakerId { get; set; }
    [ProtoMember(4)] public string? NewSpeaker { get; set; }
    [ProtoMember(5)] public string? SeriesId { get; set; }
    [ProtoMember(6)] public string? NewSeries { get; set; }
    [ProtoMember(7)] public string Service { get; set; } = "";
    [ProtoMember(8)] public DateTime PreachedOn { get; set; }
    [ProtoMember(9)] public string? YouTube { get; set; }
    [ProtoMember(10)] public MediaRef? Artwork { get; set; }
    [ProtoMember(11)] public string Summary { get; set; } = "";
    [ProtoMember(12)] public int DurationSeconds { get; set; }
    [ProtoMember(13)] public string Passage { get; set; } = "";
}

