using Gsbc.AppPoc.Data.Ministries;

namespace Gsbc.AppPoc.Data;

public sealed record Person(string Name, string? PhotoUrl = null);

public sealed record LifeGroupRound(string Name, IReadOnlyList<DateOnly> Sundays, IReadOnlyList<Ministry> Groups, string? EventHref = null)
{
    public DateOnly First => Sundays[0];

    public DateOnly Last => Sundays[^1];

    public string Dates => First.Month == Last.Month ? $"{First.Day} – {Last:d MMM}" : $"{First:d MMM} – {Last:d MMM}";
}

public sealed record SessionChange(string GroupId, DateOnly Date, string Summary, string? MovedToPlaceId = null);

public static class LifeGroupsContent
{
    public static readonly TimeOnly Start = new(9, 0);
    public static readonly TimeOnly End = new(9, 45);

    private const string Cdn = "https://dq5pwpg1q8ru0.cloudfront.net/2026/05/27/";

    private static readonly Person PhilParry = new("Phil Parry", Cdn + "01/50/25/4cb74274-e364-4179-9091-6906dd9c98dd/Parry-P_SQUARE.jpg");
    private static readonly Person AndrewBarns = new("Andrew Barns", Cdn + "01/50/25/caa12e43-7da5-4ac2-9056-3e8ef988b0d0/Barns-A_SQUARE.jpg");
    private static readonly Person DannyHolowaty = new("Danny Holowaty", Cdn + "01/50/25/ffa11d6d-a34e-4c7e-a10b-478d759ab725/Holowaty-D_SQUARE.jpg");
    private static readonly Person PeterGeorgeJnr = new("Peter George Jnr", Cdn + "01/50/25/c62685cb-0a82-4d4e-a507-185a1eb56965/George-P_SQUARE.jpg");

    public static readonly IReadOnlyList<Ministry> AllYear =
    [
        Group("nursery", "Nursery", "Care for babies and toddlers while their parents are in Life Groups.",
            [], "#fedd59", "south-block-lower", 1, "Ages 0–3", parentsJoin: true),
        Group("kids-life", "Kids Life", "Bible classes for children: Seedlings, Sprouts and Saplings.",
            [new Person("Jabez & Gazelle Wells", Cdn + "02/25/39/b902f68f-a897-4562-9b7c-92d0926a0f63/Wells-JG_SQUARE.jpg")], "#f89833", "prep-centre", 2, "Prep – Year 6", parentsJoin: true),
        Group("youth", "Youth", "Bible class for high schoolers.",
            [new Person("Brock & Chloe Morley", Cdn + "01/50/25/67f35c9e-ff72-436b-a205-6b16fcf1a9a2/Morley-BC_SQUARE.jpg")], "#6bcbd5", "youth-building", 3, "Years 7–12"),
    ];

    public static readonly LifeGroupRound Round2 = new("Round 2", Sundays(new(2026, 9, 13), 6),
    [
        Group("meaningful-marriages", "Meaningful Marriages",
            "Marriage is not just about finding love and a soul mate, but rather a purposeful union designed by God with eternal weight for His Kingdom.",
            [PhilParry], "#f0506b", "south-block-upper", 4),
        Group("jesus-god-and-man", "Jesus: God & Man",
            "Understanding who Jesus is as God and as Man, allowing us to appreciate and relate to the nature and character of our Saviour even more.",
            [AndrewBarns], "#5e7dbe", "library-east", 5),
        Group("adorning-the-heart", "Adorning The Heart",
            "An in-depth study for ladies of Peter's first epistle: what it teaches us about God's character and how we can live holy lives that please Him.",
            [new Person("Michelle Hills")], "#f38fb3", "primary-school-south", 6, "Ladies"),
        Group("victorious-christian-life", "Victorious Christian Life",
            "Victory in the Christian life is not self-confidence or outward success. It is living daily in Christ, allowing Him to shape us.",
            [DannyHolowaty, PeterGeorgeJnr], "#5ec095", "primary-school-north", 7),
        Group("subtilty-to-the-simple", "Subtilty To The Simple",
            "Exploring our responsibility as prudent guides to the simple, and how we can help guide them toward wisdom.",
            [new Person("Garry Tingson")], "#8e769f", "north-block", 8),
    ]);

    public static readonly LifeGroupRound SummerFit = new("Summer FIT", Sundays(new(2026, 11, 8), 4),
    [
        Group("summer-fit", "Summer FIT",
            "As the Life Groups year draws to a close, all adult classes combine for a four-week study to prepare us spiritually for summer: to finish the year well, not drift through it.",
            [], "#e7ae74", null, 4, "All adults"),
    ], "events/summer-fit");

    public static readonly IReadOnlyList<LifeGroupRound> Rounds = [Round2, SummerFit];

    public static readonly IReadOnlyList<SessionChange> Changes =
    [
        new("victorious-christian-life", new(2026, 10, 11), "Combined with Meaningful Marriages in room 4", "south-block-upper"),
    ];

    public static readonly IReadOnlyList<Ministry> Groups = [.. AllYear, .. Rounds.SelectMany(r => r.Groups)];

    private static readonly DateOnly AllYearFrom = new(2026, 9, 13);
    private const int AllYearSundays = 12;

    public static LifeGroupRound? RoundOf(Ministry group) => Rounds.FirstOrDefault(r => r.Groups.Contains(group));

    public static IEnumerable<MinistryPost> Posts() => Groups.SelectMany(group =>
    {
        var posted = At(RoundOf(group)?.First.AddDays(-3) ?? new(2026, 10, 18), 8);
        var author = group.Leaders.FirstOrDefault()?.Name ?? "The leaders";
        var changes = Changes.Where(c => c.GroupId == group.Id).Select(change => new MinistryPost(group.Id,
            $"This Sunday: {change.Summary.ToLowerInvariant()}",
            "Sample notice. A leader who's away posts a change like this from the app, and everyone who joined gets it as a notification.",
            At(change.Date.AddDays(-1), 18), Audience.Everyone, author, Important: true));
        return changes.Append(new(group.Id, "Sample notice",
            "Leaders post notices here: a change of room, no class this week, what to read before Sunday. Members get them as notifications.",
            posted, Audience.Everyone, author));
    });

    public static IEnumerable<MinistryFile> Files() =>
        Groups.Select(group => new MinistryFile(group.Id, "Sample: this week's notes", "Notes, handouts and readings stay here for the round", FileKind.Document, Audience.Everyone));

    public static IEnumerable<MinistryEvent> Events() => Groups.SelectMany(group =>
    {
        var round = RoundOf(group);
        var sundays = round?.Sundays ?? Sundays(AllYearFrom, AllYearSundays);
        return sundays.Select((date, index) =>
        {
            var change = Changes.FirstOrDefault(c => c.GroupId == group.Id && c.Date == date);
            var title = round is null ? "Sunday class" : index == sundays.Count - 1 ? "Last week" : $"Week {index + 1}";
            return new MinistryEvent(group.Id, title, At(date, Start.Hour), End.ToTimeSpan() - Start.ToTimeSpan(),
                Room(change?.MovedToPlaceId ?? group.LifeGroup!.PlaceId), Audience.Everyone, change?.Summary, change is not null);
        });
    });

    private static string Room(string? placeId) => placeId is { } id && SitePlan.Find(id) is { } place ? place.Name : "Room to be confirmed";

    private static DateTime At(DateOnly date, int hour) => date.ToDateTime(new TimeOnly(hour, 0));

    private static Ministry Group(string id, string title, string blurb, IReadOnlyList<Person> leaders, string colour, string? placeId, int mapNumber, string? audience = null, bool parentsJoin = false) =>
        new(id, title, blurb, [blurb], null, leaders, colour, "users", JoinMode.Open, false,
            [MinistryModule.Calendar, MinistryModule.Announcements, MinistryModule.Files],
            WhoFor: audience, WithChildren: parentsJoin, Kind: MinistryKind.LifeGroup, LifeGroup: new(placeId, mapNumber, parentsJoin));

    private static IReadOnlyList<DateOnly> Sundays(DateOnly first, int count) =>
        Enumerable.Range(0, count).Select(week => first.AddDays(7 * week)).ToList();
}
