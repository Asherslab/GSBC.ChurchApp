namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Data;

public sealed class ImportRun
{
    public const int SingleId = 1;

    public int Id { get; set; } = SingleId;
    public int Number { get; set; }
    public string State { get; set; } = Contracts.ImportStates.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int ListingPages { get; set; }
    public int? WebsiteCount { get; set; }
    public string? CurrentPath { get; set; }
    public string? Problem { get; set; }
}

public sealed class ImportedSermon
{
    public const string Waiting = "waiting";
    public const string Imported = "imported";
    public const string Failed = "failed";

    public required string WebsitePath { get; set; }
    public string Title { get; set; } = "";
    public DateOnly? PreachedOn { get; set; }
    public string State { get; set; } = Waiting;
    public int? WebsiteId { get; set; }
    public Guid? SermonId { get; set; }
    public string? Problem { get; set; }
    public long Bytes { get; set; }
    public long CopiedBytes { get; set; }
    public int SeenInRun { get; set; }
    public int? ImportedInRun { get; set; }
    public bool AlreadyThere { get; set; }
    public DateTime UpdatedAt { get; set; }
}
