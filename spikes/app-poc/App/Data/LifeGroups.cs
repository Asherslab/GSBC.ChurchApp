namespace Gsbc.AppPoc.Data;

public sealed record Person(string Name, string? PhotoUrl = null);

public sealed record LifeGroup(
    string Id,
    string Title,
    string Blurb,
    IReadOnlyList<Person> Leaders,
    string Colour,
    string? PlaceId,
    int MapNumber,
    string? Audience = null,
    bool ParentsJoin = false);

public sealed record LifeGroupRound(string Name, IReadOnlyList<DateOnly> Sundays, IReadOnlyList<LifeGroup> Groups, string? EventHref = null)
{
    public DateOnly First => Sundays[0];

    public DateOnly Last => Sundays[^1];

    public string Dates => First.Month == Last.Month ? $"{First.Day} – {Last:d MMM}" : $"{First:d MMM} – {Last:d MMM}";
}

public sealed record SessionChange(string GroupId, DateOnly Date, string Summary, string? MovedToPlaceId = null);

public enum PostKind { Notice, Resource }

public sealed record GroupPost(string GroupId, PostKind Kind, string Title, string Body, DateOnly Date, bool Urgent = false);

public static class LifeGroupsContent
{
    public static readonly TimeOnly Start = new(9, 0);
    public static readonly TimeOnly End = new(9, 45);

    private const string Cdn = "https://dq5pwpg1q8ru0.cloudfront.net/2026/05/27/";

    private static readonly Person PhilParry = new("Phil Parry", Cdn + "01/50/25/4cb74274-e364-4179-9091-6906dd9c98dd/Parry-P_SQUARE.jpg");
    private static readonly Person AndrewBarns = new("Andrew Barns", Cdn + "01/50/25/caa12e43-7da5-4ac2-9056-3e8ef988b0d0/Barns-A_SQUARE.jpg");
    private static readonly Person DannyHolowaty = new("Danny Holowaty", Cdn + "01/50/25/ffa11d6d-a34e-4c7e-a10b-478d759ab725/Holowaty-D_SQUARE.jpg");
    private static readonly Person PeterGeorgeJnr = new("Peter George Jnr", Cdn + "01/50/25/c62685cb-0a82-4d4e-a507-185a1eb56965/George-P_SQUARE.jpg");

    public static readonly IReadOnlyList<LifeGroup> AllYear =
    [
        new("nursery", "Nursery", "Care for babies and toddlers while their parents are in Life Groups.",
            [], "#fedd59", "south-block-lower", 1, "Ages 0–3", ParentsJoin: true),
        new("kids-life", "Kids Life", "Bible classes for children: Seedlings, Sprouts and Saplings.",
            [new("Jabez & Gazelle Wells", Cdn + "02/25/39/b902f68f-a897-4562-9b7c-92d0926a0f63/Wells-JG_SQUARE.jpg")], "#f89833", "prep-centre", 2, "Prep – Year 6", ParentsJoin: true),
        new("youth", "Youth", "Bible class for high schoolers.",
            [new("Brock & Chloe Morley", Cdn + "01/50/25/67f35c9e-ff72-436b-a205-6b16fcf1a9a2/Morley-BC_SQUARE.jpg")], "#6bcbd5", "youth-building", 3, "Years 7–12"),
    ];

    public static readonly LifeGroupRound Round2 = new("Round 2", Sundays(new(2026, 9, 13), 6),
    [
        new("meaningful-marriages", "Meaningful Marriages",
            "Marriage is not just about finding love and a soul mate, but rather a purposeful union designed by God with eternal weight for His Kingdom.",
            [PhilParry], "#f0506b", "south-block-upper", 4),
        new("jesus-god-and-man", "Jesus: God & Man",
            "Understanding who Jesus is as God and as Man, allowing us to appreciate and relate to the nature and character of our Saviour even more.",
            [AndrewBarns], "#5e7dbe", "library-east", 5),
        new("adorning-the-heart", "Adorning The Heart",
            "An in-depth study for ladies of Peter's first epistle: what it teaches us about God's character and how we can live holy lives that please Him.",
            [new("Michelle Hills")], "#f38fb3", "primary-school-south", 6, "Ladies"),
        new("victorious-christian-life", "Victorious Christian Life",
            "Victory in the Christian life is not self-confidence or outward success. It is living daily in Christ, allowing Him to shape us.",
            [DannyHolowaty, PeterGeorgeJnr], "#5ec095", "primary-school-north", 7),
        new("subtilty-to-the-simple", "Subtilty To The Simple",
            "Exploring our responsibility as prudent guides to the simple, and how we can help guide them toward wisdom.",
            [new("Garry Tingson")], "#8e769f", "north-block", 8),
    ]);

    public static readonly LifeGroupRound SummerFit = new("Summer FIT", Sundays(new(2026, 11, 8), 4),
    [
        new("summer-fit", "Summer FIT",
            "As the Life Groups year draws to a close, all adult classes combine for a four-week study to prepare us spiritually for summer: to finish the year well, not drift through it.",
            [], "#e7ae74", null, 4, "All adults"),
    ], "events/summer-fit");

    public static readonly IReadOnlyList<LifeGroupRound> Rounds = [Round2, SummerFit];

    public static readonly IReadOnlyList<SessionChange> Changes =
    [
        new("victorious-christian-life", new(2026, 10, 11), "Combined with Meaningful Marriages in room 4", "south-block-upper"),
    ];

    public static IReadOnlyList<GroupPost> PostsFor(LifeGroup group, LifeGroupRound? round)
    {
        var posted = round?.Sundays[^2] ?? new(2026, 10, 18);
        var posts = new List<GroupPost>();
        foreach (var change in Changes.Where(c => c.GroupId == group.Id))
        {
            posts.Add(new(group.Id, PostKind.Notice, $"This Sunday: {change.Summary.ToLowerInvariant()}",
                "Sample notice. A leader who's away posts a change like this from the app, and everyone who joined gets it as a notification.",
                change.Date.AddDays(-1), Urgent: true));
        }
        posts.Add(new(group.Id, PostKind.Notice, "Sample notice",
            "Leaders post notices here: a change of room, no class this week, what to read before Sunday. Members get them as notifications.",
            posted));
        posts.Add(new(group.Id, PostKind.Resource, "Sample: this week's notes",
            "Notes, handouts and readings a leader shares appear here for the rest of the round.",
            posted));
        return posts;
    }

    private static IReadOnlyList<DateOnly> Sundays(DateOnly first, int count) =>
        Enumerable.Range(0, count).Select(week => first.AddDays(7 * week)).ToList();
}
