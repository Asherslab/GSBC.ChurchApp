using System.Globalization;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistryWords
{
    public static readonly IReadOnlyList<string> BoxOrder = [Audiences.Team, Audiences.Attendees, Audiences.Parents, Audiences.Everyone];

    public static string Box(string audience) => audience switch
    {
        Audiences.Team => "For the team",
        Audiences.Attendees => "For families",
        Audiences.Parents => "For parents",
        _ => "For everyone",
    };

    public static string Short(string audience) => audience switch
    {
        Audiences.Team => "The team",
        Audiences.Attendees => "Attendees and parents",
        Audiences.Parents => "Parents",
        _ => "Everyone",
    };

    public static string WhoReads(string audience, MinistryDetails ministry) => audience switch
    {
        Audiences.Team => $"Only the {ministry.Name} team sees this.",
        Audiences.Attendees => $"Attendees of {ministry.Name}, their parents and the team see this.",
        Audiences.Parents => $"Only parents of attendees of {ministry.Name}, and the team, see this.",
        _ when MinistryLogic.IsHidden(ministry) => $"{ministry.Name} is hidden, so only its own people, the leaders above it and staff see this.",
        _ => "Anyone with the app sees this, including visitors.",
    };

    public static string WhoSeesHidden(string ministry) =>
        $"Only its team, its attendees and their parents, the leaders above it and staff who look after ministries see {ministry}. "
        + "It isn't in the list of ministries, search or Home for anyone else, and nobody else can ask to join. The ministries under it are hidden too.";

    public static string HiddenBecause(string above) => $"Hidden, because {above} is. Only its own people and staff see it.";

    public static string Short(string audience, MinistryDetails ministry) =>
        audience == Audiences.Everyone && MinistryLogic.IsHidden(ministry) ? "Everyone who sees it" : Short(audience);

    public static string PostFor(string audience, MinistryDetails ministry) =>
        audience == Audiences.Everyone && MinistryLogic.IsHidden(ministry) ? "Post for everyone who sees it" : PostFor(audience);

    public static string PostFor(string audience) => audience switch
    {
        Audiences.Team => "Post for the team",
        Audiences.Attendees => "Post for attendees and parents",
        Audiences.Parents => "Post for parents",
        _ => "Post for everyone",
    };

    public static string BoxClass(string audience) => audience switch
    {
        Audiences.Team => "ministry-box--team",
        Audiences.Attendees => "ministry-box--families",
        Audiences.Parents => "ministry-box--parents",
        _ => "ministry-box--everyone",
    };

    public static string Role(string role) => role switch
    {
        Roles.Leader => "Leader",
        Roles.Admin => "Admin",
        Roles.Team => "Team",
        _ => "Attends",
    };

    public static string Allows(string role) => role switch
    {
        Roles.Leader => "Leads it and everything under it, and is listed as a leader",
        Roles.Admin => "Can do everything a leader can, without being listed as one",
        Roles.Team => "Sees what's for the team: its news, files, chats and roster",
        _ => "Comes along, and sees what's for attendees and their families",
    };

    public static string HeroNote(MinistryDetails ministry) => ministry.Summary.Length > 0 ? ministry.Summary : ministry.Meets;

    public static string JoinLine(MinistrySummary ministry) => ministry.TeamJoin switch
    {
        Joining.Open => "Open to join",
        Joining.Ask => "Ask to join",
        _ => "",
    };

    public static string Yours(MinistryPage page)
    {
        YourPlace you = page.You;
        List<string> attending = you.Children.Where(child => child.Attends).Select(child => child.Name).ToList();
        if (you.Roles.Contains(Roles.Leader) || you.LeadsFromAbove)
            return "You lead it";
        if (you.Roles.Contains(Roles.Admin))
            return "You look after it";
        if (you.Roles.Contains(Roles.Team))
            return "On the team";
        if (attending.Count > 0)
            return $"{Names(attending)} {(attending.Count == 1 ? "attends" : "attend")}";
        return you.Roles.Contains(Roles.Attendee) ? "You come along" : "";
    }

    public static string Possessive(string name) => MinistryNames.Possessive(name);

    public static string Ministries(int count) => count == 1 ? "1 ministry" : $"{count} ministries";

    public static string Names(IReadOnlyList<string> names) =>
        names.Count <= 1 ? names.FirstOrDefault() ?? "" : $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";

    public static string Ago(DateTime at, DateTime now)
    {
        TimeSpan gone = now - at;
        return gone.TotalHours < 1 ? "Just now"
            : gone.TotalDays < 1 ? "Today"
            : gone.TotalDays < 2 ? "Yesterday"
            : gone.TotalDays < 7 ? $"{(int)gone.TotalDays} days ago"
            : gone.TotalDays < 14 ? "Last week"
            : at.ToLocalTime().ToString("d MMMM", CultureInfo.CurrentCulture);
    }

    public static IReadOnlyList<DayRange> Ranges(IEnumerable<DaySpan> spans) =>
        spans.Select(span => new DayRange(LifeGroupRounds.Day(span.From), LifeGroupRounds.Day(span.To))).ToList();

    public static string Span(IReadOnlyList<DaySpan> spans) =>
        spans.Count == 0 ? "" : $"{ShortDay(spans[0].From)} to {ShortDay(spans[^1].To)}";

    private static string ShortDay(string isoDate) =>
        DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day)
            ? day.ToString("d MMM", CultureInfo.CurrentCulture)
            : isoDate;

    public static string Day(string isoDate) =>
        DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day)
            ? day.ToString("d MMMM", CultureInfo.CurrentCulture)
            : isoDate;

    public static string NotOn(SittingOut off) =>
        string.Join(" · ", new[] { off.Round is { } round ? $"Not on {Span(round)}" : "Not on these Sundays", off.Note }.Where(part => part.Length > 0));

    public static string Span(Round round) => Range(round, "MMMM");

    public static string Short(Round round) => Range(round, "MMM");

    private static string Range(Round round, string month)
    {
        if (round.Sundays.Count == 0)
            return "";
        DateOnly first = LifeGroupRounds.Day(round.Sundays[0]), last = LifeGroupRounds.Day(round.Sundays[^1]);
        string end = last.ToString($"d {month}", CultureInfo.CurrentCulture);
        return first == last ? end
            : first.Month == last.Month && first.Year == last.Year ? $"{first.Day} to {end}"
            : $"{first.ToString($"d {month}", CultureInfo.CurrentCulture)} to {end}";
    }

    public static string Time(string hhmm) =>
        TimeOnly.TryParseExact(hhmm, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly time)
            ? time.ToString(time.Minute == 0 ? "h tt" : "h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant().Replace(" ", "")
            : hhmm;

    public static string AskTitle(AskLine ask) =>
        ask.As == Places.Team ? $"{ask.Name} asks to join the team" : $"{ask.Name}{(ask.Age is { } age ? $", {age}," : "")} asks to come along";

    public static string AskDetail(AskLine ask, DateTime now)
    {
        List<string> parts = [];
        if (ask.AskedBy.Length > 0)
            parts.Add($"Asked by {ask.AskedBy}");
        parts.Add(Ago(ask.AskedAt, now));
        return string.Join(" · ", parts);
    }
}
