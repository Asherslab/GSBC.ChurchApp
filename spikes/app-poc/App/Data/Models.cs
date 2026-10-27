namespace Gsbc.AppPoc.Data;

public sealed record ServiceTime(DayOfWeek Day, TimeOnly Start, TimeSpan Length, string Name, bool Streamed, string? Href = null);

public sealed record OutlineItem(string Text, params string[] Passages);

public sealed record OutlinePoint(string Heading, params OutlineItem[] Items);

public sealed record SermonNotes(string? MainText, IReadOnlyList<OutlinePoint> Outline);

public sealed record Sermon(
    string Id,
    string Title,
    string? Part,
    string Speaker,
    DateOnly Date,
    string Service,
    string? Series,
    string? YouTubeId,
    string? AudioUrl,
    string? ImageUrl,
    string Link);

public sealed record AgendaItem(string Time, string Name);

public sealed record AgendaDay(DayOfWeek Day, IReadOnlyList<AgendaItem> Items);

public sealed record Announcement(string Id, string Kicker, string Title, string Body, string? ActionLabel, string? ActionHref);

public sealed record Letter(string Heading, IReadOnlyList<string> Paragraphs, string Challenge, string SignOff);

public sealed record Bulletin(
    DateOnly Date,
    string Theme,
    string ThemeVerse,
    Letter Welcome,
    IReadOnlyList<string> FocusSermonIds,
    IReadOnlyList<AgendaDay> Week,
    IReadOnlyList<Announcement> WhatsNext);

public enum EventCategory { Church, Adults, Kids, Youth, Prayer }

public sealed record ChurchEvent(
    string Id,
    string Title,
    IReadOnlyList<string> Description,
    DateTime Start,
    TimeSpan Length,
    string Location,
    EventCategory Category,
    string? ImageUrl,
    string? Repeats = null,
    string? RegisterHref = null);

public sealed record PrayerPost(string Text, string When, int Praying);

public sealed record NotificationTopic(string Id, string Name, string Description);

public sealed record InboxMessage(string Title, string Body, string When, bool Unread, string Href);
