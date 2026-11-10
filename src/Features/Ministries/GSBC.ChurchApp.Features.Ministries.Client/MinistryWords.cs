using System.Globalization;
using GSBC.ChurchApp.Features.Ministries.Contracts;

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

    public static string WhoReads(string audience, string ministry) => audience switch
    {
        Audiences.Team => $"Only the {ministry} team sees this.",
        Audiences.Attendees => $"Attendees of {ministry}, their parents and the team see this.",
        Audiences.Parents => $"Only parents of attendees of {ministry}, and the team, see this.",
        _ => "Anyone with the app sees this, including visitors.",
    };

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

    public static string HeroNote(MinistryDetails ministry) => ministry.Summary.Length > 0 ? ministry.Summary : ministry.Meets;

    public static string JoinLine(MinistrySummary ministry) => (ministry.Kind == MinistryKinds.LifeGroup ? ministry.AttendJoin : ministry.TeamJoin) switch
    {
        Joining.Open => "Open to join",
        Joining.Ask => "Ask to join",
        _ => "Invite only",
    };

    public static string UnderLine(MinistrySummary ministry) =>
        ministry.Kind == MinistryKinds.LifeGroup
            ? string.Join(" · ", new[] { "Life Group", ministry.Summary }.Where(part => part.Length > 0))
            : JoinLine(ministry);

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

    public static string Day(string isoDate) =>
        DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day)
            ? day.ToString("d MMMM", CultureInfo.CurrentCulture)
            : isoDate;

    public static string Span(Round round) =>
        round.Sundays.Count == 0 ? "" : round.Sundays.Count == 1 ? Day(round.Sundays[0]) : $"{Day(round.Sundays[0])} to {Day(round.Sundays[^1])}";

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
