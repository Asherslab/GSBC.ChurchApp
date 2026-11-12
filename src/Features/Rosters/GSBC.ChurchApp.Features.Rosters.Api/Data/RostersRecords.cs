namespace GSBC.ChurchApp.Features.Rosters.Api.Data;

public sealed class PositionRecord
{
    public Guid Id { get; set; }
    public Guid MinistryId { get; set; }
    public string Name { get; set; } = "";
    public int Order { get; set; }
    public int Needed { get; set; } = 1;
    public bool NeedsBlueCard { get; set; }
    public bool NeedsTraining { get; set; }
    public bool NeedsTrained { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class TrainedRecord
{
    public Guid PositionId { get; set; }
    public Guid PersonId { get; set; }
    public Guid AddedBy { get; set; }
    public DateTime AddedAt { get; set; }
}

public sealed class SettingsRecord
{
    public Guid MinistryId { get; set; }
    public bool Replies { get; set; } = true;
    public string OpenSwaps { get; set; } = Contracts.OpenSwaps.Team;
    public bool DirectSwaps { get; set; }
    public List<Guid> Calendars { get; set; } = [];
    public string WhoSees { get; set; } = "";
    public int RemindDaysBefore { get; set; }
    public int NudgeAfterDays { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class DateRecord
{
    public Guid Id { get; set; }
    public Guid MinistryId { get; set; }
    public string EventKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string CalendarName { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool AllDay { get; set; }
    public bool NotOn { get; set; }
    public bool Gone { get; set; }
    public Guid AddedBy { get; set; }
    public DateTime AddedAt { get; set; }
}

public sealed class AssignmentRecord
{
    public Guid Id { get; set; }
    public Guid DateId { get; set; }
    public Guid MinistryId { get; set; }
    public Guid PositionId { get; set; }
    public string PositionName { get; set; } = "";
    public Guid? PersonId { get; set; }
    public string State { get; set; } = "";
    public string Reason { get; set; } = "";
    public Guid? RepliedBy { get; set; }
    public DateTime? RepliedAt { get; set; }
    public Guid AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? RemindedAt { get; set; }
    public DateTime? NudgedAt { get; set; }
}

public sealed class SwapRecord
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid MinistryId { get; set; }
    public string Way { get; set; } = "";
    public string State { get; set; } = "";
    public Guid AskedBy { get; set; }
    public Guid? AskedTo { get; set; }
    public Guid? OfferedBy { get; set; }
    public string Note { get; set; } = "";
    public DateTime AskedAt { get; set; }
    public DateTime? SettledAt { get; set; }
}

public sealed class AwayRecord
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public DateOnly From { get; set; }
    public DateOnly? To { get; set; }
    public bool Weekly { get; set; }
    public List<int> Weekdays { get; set; } = [];
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public List<DateOnly> Except { get; set; } = [];
    public string Note { get; set; } = "";
    public Guid MadeBy { get; set; }
    public DateTime MadeAt { get; set; }
}

public sealed class PublishedRecord
{
    public string Feed { get; set; } = "";
    public string RecordId { get; set; } = "";
    public Guid ScopeId { get; set; }
    public Guid? MinistryId { get; set; }
    public string Hash { get; set; } = "";
}
