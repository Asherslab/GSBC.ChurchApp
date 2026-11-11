using GSBC.ChurchApp.Features.Prayer.Contracts;

namespace GSBC.ChurchApp.Features.Prayer.Api.Data;

public sealed class RequestRecord
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
    public DateTime SentAt { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid? InstallId { get; set; }
    public Guid? UserId { get; set; }
    public string Name { get; set; } = "";
    public string Contact { get; set; } = "";
    public bool WantsContact { get; set; }
    public bool Minor { get; set; }
    public bool ShowName { get; set; }
    public CareState Care { get; set; }
    public string AnsweredNote { get; set; } = "";
    public WallState Wall { get; set; }
    public string WallText { get; set; } = "";
    public bool WallShowsName { get; set; }
    public DateTime? WallShownAt { get; set; }
    public DateTime? WallUntil { get; set; }
    public int Prayed { get; set; }
    public List<NoteRecord> Notes { get; set; } = [];
    public List<StepRecord> Steps { get; set; } = [];
}

public sealed class NoteRecord
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = "";
    public DateTime At { get; set; }
}

public sealed class StepRecord
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid? WhoId { get; set; }
    public string What { get; set; } = "";
    public DateTime At { get; set; }
}

public sealed class PrayedRecord
{
    public Guid RequestId { get; set; }
    public Guid InstallId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime At { get; set; }
}

public sealed class HandledRequestRecord
{
    public required string Id { get; set; }
    public DateTime HandledAt { get; set; }
}
