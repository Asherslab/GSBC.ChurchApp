namespace GSBC.ChurchApp.Features.Ministries.Api.Data;

public sealed class MinistryRecord
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public string About { get; set; } = "";
    public string Meets { get; set; } = "";
    public string WhoFor { get; set; } = "";
    public string Colour { get; set; } = "";
    public string Icon { get; set; } = "";
    public string TeamJoin { get; set; } = "";
    public string AttendJoin { get; set; } = "";
    public bool ShowsInLifeGroups { get; set; }
    public Guid? RoundId { get; set; }
    public int Order { get; set; }
    public List<string> PartsOff { get; set; } = [];
    public List<string> PartsFromBelow { get; set; } = [];
    public List<string> PartsOrder { get; set; } = [];
    public string ChatStarters { get; set; } = Contracts.ChatStarters.Leaders;
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public MinistryRecord Under(Guid? parentId)
    {
        MinistryRecord moved = (MinistryRecord)MemberwiseClone();
        moved.ParentId = parentId;
        return moved;
    }
}

public sealed class AskRecord
{
    public Guid Id { get; set; }
    public Guid MinistryId { get; set; }
    public Guid PersonId { get; set; }
    public string As { get; set; } = "";
    public Guid AskedBy { get; set; }
    public string Note { get; set; } = "";
    public DateTime AskedAt { get; set; }
    public bool WaitingForParent { get; set; }
}

public sealed class PostRecord
{
    public Guid Id { get; set; }
    public Guid MinistryId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string Audience { get; set; } = "";
    public bool Important { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public DateTime PostedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? WholeChurchAskedAt { get; set; }
}

public sealed class FileRecord
{
    public Guid Id { get; set; }
    public Guid MinistryId { get; set; }
    public string Title { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Url { get; set; } = "";
    public Guid? MediaId { get; set; }
    public string Audience { get; set; } = "";
    public string Note { get; set; } = "";
    public Guid AddedBy { get; set; }
    public DateTime AddedAt { get; set; }
}

public sealed class RoundRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<DateOnly> Sundays { get; set; } = [];
    public TimeOnly Starts { get; set; }
    public TimeOnly Ends { get; set; }
    public DateOnly? ShowsFrom { get; set; }
}

public sealed class SitOutRecord
{
    public Guid GroupId { get; set; }
    public Guid RoundId { get; set; }
    public string Note { get; set; } = "";
    public Guid? InsteadId { get; set; }
    public DateTime MarkedAt { get; set; }
}

public sealed class SessionRecord
{
    public Guid GroupId { get; set; }
    public DateOnly Date { get; set; }
    public bool Off { get; set; }
    public string Note { get; set; } = "";
}
