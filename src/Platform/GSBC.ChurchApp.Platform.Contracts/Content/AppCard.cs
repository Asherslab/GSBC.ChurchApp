using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public enum AppCardKind
{
    Event,
    Sermon,
    Ministry,
    LifeGroup,
}

public enum EventShows
{
    OneDate,
    NextDate,
    Pattern,
}

public enum AppCardLook
{
    Rule,
    Line,
    Box,
}

public sealed record AppCard(AppCardKind Kind, string Id, EventShows Shows = EventShows.OneDate, AppCardLook Look = AppCardLook.Rule)
{
    public string Directive
    {
        get
        {
            string shows = Shows == EventShows.OneDate ? "" : $" show={AppCards.ShowsWord(Shows)}";
            string look = Look == AppCardLook.Rule ? "" : $" look={AppCards.LookWord(Look)}";
            return $"{RichTextParser.DirectivePrefix}{AppCards.NameOf(Kind)}{{id={Id}{shows}{look}}}";
        }
    }
}

public static partial class AppCards
{
    private const string IdKey = "id";
    private const string ShowsKey = "show";
    private const string LookKey = "look";

    private static readonly IReadOnlyDictionary<string, AppCardKind> Kinds = new Dictionary<string, AppCardKind>(StringComparer.Ordinal)
    {
        ["event"] = AppCardKind.Event,
        ["sermon"] = AppCardKind.Sermon,
        ["ministry"] = AppCardKind.Ministry,
        ["lifegroup"] = AppCardKind.LifeGroup,
    };

    public static IReadOnlyList<AppCardKind> All { get; } = Enum.GetValues<AppCardKind>();

    public static string NameOf(AppCardKind kind) => Kinds.First(pair => pair.Value == kind).Key;

    public static string Noun(AppCardKind kind) => kind switch
    {
        AppCardKind.Event => "event",
        AppCardKind.Sermon => "sermon",
        AppCardKind.Ministry => "ministry",
        _ => "Life Group",
    };

    public static string WithArticle(AppCardKind kind) => kind == AppCardKind.Event ? "an event" : $"a {Noun(kind)}";

    public static string Title(AppCardKind kind) => kind == AppCardKind.LifeGroup ? "Life Group" : char.ToUpperInvariant(Noun(kind)[0]) + Noun(kind)[1..];

    public static AppCard? Of(DirectiveBlock directive) => Read(directive).Card;

    public static AppCard? OfLine(string line) =>
        RichTextParser.Parse(line).Blocks is [DirectiveBlock directive] ? Of(directive) : null;

    public static string? ProblemWith(DirectiveBlock directive) => Read(directive).Problem;

    internal static string ShowsWord(EventShows shows) => shows == EventShows.NextDate ? "next" : "pattern";

    internal static string LookWord(AppCardLook look) => look == AppCardLook.Line ? "line" : "box";

    private static (AppCard? Card, string? Problem) Read(DirectiveBlock directive)
    {
        if (!Kinds.TryGetValue(directive.Name, out AppCardKind kind))
            return (null, $"'{RichTextParser.DirectivePrefix}{directive.Name}' isn't something the app can show.");
        string broken = $"A card for {WithArticle(kind)} has lost what it points to. Take it out and choose it again.";
        if (directive.Attributes.Keys.Any(key => key is not (IdKey or ShowsKey or LookKey)))
            return (null, broken);
        if (!directive.Attributes.TryGetValue(IdKey, out string? id) || !SafeId().IsMatch(id))
            return (null, broken);
        EventShows? shows = (kind, directive.Attributes.GetValueOrDefault(ShowsKey)) switch
        {
            (_, null) => EventShows.OneDate,
            (AppCardKind.Event, "next") => EventShows.NextDate,
            (AppCardKind.Event, "pattern") => EventShows.Pattern,
            _ => null,
        };
        AppCardLook? look = directive.Attributes.GetValueOrDefault(LookKey) switch
        {
            null => AppCardLook.Rule,
            "line" => AppCardLook.Line,
            "box" => AppCardLook.Box,
            _ => null,
        };
        return shows is { } chosen && look is { } drawn ? (new AppCard(kind, id, chosen, drawn), null) : (null, broken);
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex SafeId();
}
