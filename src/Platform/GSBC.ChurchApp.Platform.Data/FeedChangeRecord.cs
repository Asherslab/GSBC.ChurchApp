namespace GSBC.ChurchApp.Platform.Data;

public sealed class FeedChangeRecord
{
    public long Cursor { get; set; }
    public required string Feed { get; set; }
    public required string RecordId { get; set; }
    public long Version { get; set; }
    public bool Deleted { get; set; }
    public byte[] Payload { get; set; } = [];
    public string? SortKey { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? GroupKey { get; set; }
    public string? ScopeId { get; set; }
    public DateTime CommittedAt { get; set; }

    public static FeedChangeRecord Marker(string feed, string recordId, DateTime at, bool deleted = false) =>
        new() { Feed = feed, RecordId = recordId, Version = at.Ticks, Deleted = deleted };
}
