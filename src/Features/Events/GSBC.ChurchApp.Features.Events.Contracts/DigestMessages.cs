using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public enum DigestItemKind
{
    Event = 0,
    NotOn = 1,
    Own = 2,
}

public static class DigestFields
{
    public const string Label = "label";
    public const string Title = "title";
    public const string When = "when";
    public const string Time = "time";
    public const string Description = "description";
    public const string Register = "register";

    public static readonly IReadOnlyList<string> All = [Label, Title, When, Time, Description, Register];
}

[ProtoContract]
public sealed class DigestWords
{
    [ProtoMember(1)] public string Label { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public string When { get; set; } = "";
    [ProtoMember(4)] public string Time { get; set; } = "";
    [ProtoMember(5)] public string Description { get; set; } = "";
    [ProtoMember(6)] public string RegisterLink { get; set; } = "";

    public string this[string field] => field switch
    {
        DigestFields.Label => Label,
        DigestFields.Title => Title,
        DigestFields.When => When,
        DigestFields.Time => Time,
        DigestFields.Description => Description,
        DigestFields.Register => RegisterLink,
        _ => "",
    };

    public DigestWords With(string field, string value)
    {
        DigestWords copy = Copy();
        switch (field)
        {
            case DigestFields.Label: copy.Label = value; break;
            case DigestFields.Title: copy.Title = value; break;
            case DigestFields.When: copy.When = value; break;
            case DigestFields.Time: copy.Time = value; break;
            case DigestFields.Description: copy.Description = value; break;
            case DigestFields.Register: copy.RegisterLink = value; break;
        }
        return copy;
    }

    public DigestWords Copy() => new() { Label = Label, Title = Title, When = When, Time = Time, Description = Description, RegisterLink = RegisterLink };

    public static DigestWords Overlay(DigestWords calendar, DigestWords own, IEnumerable<string> changed) =>
        changed.Aggregate(calendar.Copy(), (words, field) => words.With(field, own[field]));
}

[ProtoContract]
public sealed class DigestItem
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public DigestItemKind Kind { get; set; }
    [ProtoMember(3)] public bool Included { get; set; }
    [ProtoMember(4)] public bool IsNew { get; set; }
    [ProtoMember(5, IsRequired = true)] public DigestWords CalendarWords { get; set; } = new();
    [ProtoMember(6, IsRequired = true)] public DigestWords Words { get; set; } = new();
    [ProtoMember(7)] public List<string> Changed { get; set; } = [];
    [ProtoMember(8)] public MediaRef? Picture { get; set; }
    [ProtoMember(9)] public string Place { get; set; } = "";
    [ProtoMember(10)] public string Contacts { get; set; } = "";
    [ProtoMember(11)] public List<string> OccurrenceIds { get; set; } = [];
    [ProtoMember(12)] public string FirstDate { get; set; } = "";
    [ProtoMember(13)] public string LastDate { get; set; } = "";
    [ProtoMember(14)] public string CalendarId { get; set; } = "";
    [ProtoMember(15)] public string CalendarName { get; set; } = "";
    [ProtoMember(16)] public bool ForEveryone { get; set; }
    [ProtoMember(17)] public List<string> Missing { get; set; } = [];
    [ProtoMember(18)] public List<string> CalendarIds { get; set; } = [];

    public DigestWords Shown => DigestWords.Overlay(CalendarWords, Words, Changed);

    public bool Reworded => Changed.Count > 0 && Kind != DigestItemKind.Own;
}

[ProtoContract]
public sealed class DigestSent
{
    [ProtoMember(1)] public string Start { get; set; } = "";
    [ProtoMember(2)] public DateTime SentAt { get; set; }
}

[ProtoContract]
public sealed class DigestDraft
{
    [ProtoMember(1)] public string Start { get; set; } = "";
    [ProtoMember(2)] public string End { get; set; } = "";
    [ProtoMember(3)] public int Weeks { get; set; }
    [ProtoMember(4)] public List<string> CalendarIds { get; set; } = [];
    [ProtoMember(5)] public List<CalendarInfo> Calendars { get; set; } = [];
    [ProtoMember(6)] public List<DigestItem> Items { get; set; } = [];
    [ProtoMember(7)] public DateTime? SavedAt { get; set; }
    [ProtoMember(8)] public DateTime? SentAt { get; set; }
    [ProtoMember(9)] public bool SentAsItIs { get; set; }
    [ProtoMember(10)] public DigestSent? LastSent { get; set; }
}

[ProtoContract]
public sealed class DigestQuery
{
    [ProtoMember(1)] public string Start { get; set; } = "";
}

[ProtoContract]
public sealed class DigestEntry
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public bool Included { get; set; }
    [ProtoMember(3)] public List<string> Changed { get; set; } = [];
    [ProtoMember(4, IsRequired = true)] public DigestWords Words { get; set; } = new();
    [ProtoMember(5)] public bool Own { get; set; }
}

[ProtoContract]
public sealed class DigestSave
{
    [ProtoMember(1)] public string Start { get; set; } = "";
    [ProtoMember(2)] public int Weeks { get; set; }
    [ProtoMember(3)] public List<string> CalendarIds { get; set; } = [];
    [ProtoMember(4)] public List<DigestEntry> Entries { get; set; } = [];
}

[ProtoContract]
public sealed class DigestRef
{
    [ProtoMember(1)] public string Start { get; set; } = "";
}

[ProtoContract]
public sealed class DigestShown
{
    [ProtoMember(1)] public string Start { get; set; } = "";
    [ProtoMember(2)] public string End { get; set; } = "";
    [ProtoMember(3)] public DateTime SentAt { get; set; }
}

[ProtoContract]
public sealed class DigestShownItem
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public int Order { get; set; }
    [ProtoMember(3)] public DigestItemKind Kind { get; set; }
    [ProtoMember(4, IsRequired = true)] public DigestWords Words { get; set; } = new();
    [ProtoMember(5)] public MediaRef? Picture { get; set; }
    [ProtoMember(6)] public string Place { get; set; } = "";
    [ProtoMember(7)] public string Contacts { get; set; } = "";
    [ProtoMember(8)] public string OccurrenceId { get; set; } = "";
    [ProtoMember(9)] public string FirstDate { get; set; } = "";
    [ProtoMember(10)] public string LastDate { get; set; } = "";
}
