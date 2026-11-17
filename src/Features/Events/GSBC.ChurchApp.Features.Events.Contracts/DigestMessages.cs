using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public enum DigestRange
{
    TwoWeeks = 0,
    OneWeek = 1,
    Month = 2,
    Custom = 3,
}

public enum DigestItemKind
{
    Event = 0,
    NotOn = 1,
    Own = 2,
    News = 3,
}

public enum DigestPictureAt
{
    Beside = 0,
    Across = 1,
}

public enum DigestHeading
{
    TitleAndDates = 0,
    DatesOnly = 1,
    Nothing = 2,
}

public static class DigestFields
{
    public const string Label = "label";
    public const string Title = "title";
    public const string When = "when";
    public const string Time = "time";
    public const string Description = "description";
    public const string Register = "register";
    public const string Picture = "picture";

    public static readonly IReadOnlyList<string> All = [Label, Title, When, Time, Description, Register, Picture];
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
    [ProtoMember(7)] public MediaRef? Picture { get; set; }

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

    public DigestWords WithPicture(MediaRef? picture)
    {
        DigestWords copy = Copy();
        copy.Picture = picture;
        return copy;
    }

    public DigestWords Copy() => new() { Label = Label, Title = Title, When = When, Time = Time, Description = Description, RegisterLink = RegisterLink, Picture = Picture };

    public static DigestWords Overlay(DigestWords calendar, DigestWords own, IReadOnlyCollection<string> changed)
    {
        DigestWords words = changed.Aggregate(calendar.Copy(), (overlaid, field) => overlaid.With(field, own[field]));
        return changed.Contains(DigestFields.Picture) ? words.WithPicture(own.Picture) : words;
    }
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
    [ProtoMember(19)] public bool Special { get; set; }
    [ProtoMember(20)] public string Link { get; set; } = "";
    [ProtoMember(21)] public string Posted { get; set; } = "";
    [ProtoMember(22)] public string OnlyFor { get; set; } = "";
    [ProtoMember(23)] public DigestPictureAt PictureAt { get; set; }

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
    [ProtoMember(4)] public List<string> CalendarIds { get; set; } = [];
    [ProtoMember(5)] public List<CalendarInfo> Calendars { get; set; } = [];
    [ProtoMember(6)] public List<DigestItem> Items { get; set; } = [];
    [ProtoMember(7)] public DateTime? SavedAt { get; set; }
    [ProtoMember(8)] public DateTime? SentAt { get; set; }
    [ProtoMember(9)] public bool SentAsItIs { get; set; }
    [ProtoMember(10)] public DigestSent? LastSent { get; set; }
    [ProtoMember(11)] public DateTime? NotifiedAt { get; set; }
    [ProtoMember(12)] public string Id { get; set; } = "";
    [ProtoMember(13)] public DigestRange Range { get; set; }
    [ProtoMember(14)] public List<DigestItem> Calendar { get; set; } = [];
    [ProtoMember(15)] public List<DigestItem> Posts { get; set; } = [];
    [ProtoMember(16)] public MediaRef? TopPicture { get; set; }
    [ProtoMember(17)] public DigestHeading Heading { get; set; }
}

[ProtoContract]
public sealed class DigestQuery
{
    [ProtoMember(2)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class DigestEntry
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public bool Included { get; set; }
    [ProtoMember(3)] public List<string> Changed { get; set; } = [];
    [ProtoMember(4, IsRequired = true)] public DigestWords Words { get; set; } = new();
    [ProtoMember(5)] public bool Own { get; set; }
    [ProtoMember(6)] public DigestPictureAt PictureAt { get; set; }
}

[ProtoContract]
public sealed class DigestSave
{
    [ProtoMember(1)] public string Start { get; set; } = "";
    [ProtoMember(3)] public List<string> CalendarIds { get; set; } = [];
    [ProtoMember(4)] public List<DigestEntry> Entries { get; set; } = [];
    [ProtoMember(5)] public string Id { get; set; } = "";
    [ProtoMember(6)] public DigestRange Range { get; set; }
    [ProtoMember(7)] public string End { get; set; } = "";
    [ProtoMember(8)] public MediaRef? TopPicture { get; set; }
    [ProtoMember(9)] public DigestHeading Heading { get; set; }
}

[ProtoContract]
public sealed class DigestRef
{
    [ProtoMember(2)] public bool NotifyAgain { get; set; }
    [ProtoMember(3)] public string Id { get; set; } = "";
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
    [ProtoMember(11)] public string Link { get; set; } = "";
    [ProtoMember(12)] public string Posted { get; set; } = "";
}
