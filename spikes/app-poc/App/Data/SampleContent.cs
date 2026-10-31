namespace Gsbc.AppPoc.Data;

public sealed class SampleContent
{
    public const string ChurchName = "Good Shepherd Baptist Church";
    public const string Address = "185 Old Northern Road, Albany Creek QLD 4035";
    public const string Phone = "07 3264 3476";
    public const string PhoneHref = "tel:+61732643476";
    public const string Email = "info@baptist.com.au";
    public const string Website = "https://baptist.com.au";
    public const string GiveHref = "https://baptist.com.au/give";
    public const string YouTubeChannel = "https://www.youtube.com/@gsbc.brisbane";
    public const string DirectionsHref = "https://maps.apple.com/?daddr=185+Old+Northern+Rd,+Albany+Creek+QLD+4035";

    private const string Cdn = "https://dq5pwpg1q8ru0.cloudfront.net/2026/";

    public required IReadOnlyList<ServiceTime> Services { get; init; }
    public required IReadOnlyDictionary<string, SermonNotes> SermonNotes { get; init; }
    public required IReadOnlyDictionary<string, string> SpeakerPhotos { get; init; }
    public required Bulletin Bulletin { get; init; }
    public required IReadOnlyList<ChurchEvent> Events { get; init; }
    public required IReadOnlyList<PrayerPost> PrayerWall { get; init; }
    public required IReadOnlyList<NotificationTopic> Topics { get; init; }
    public required IReadOnlyList<InboxMessage> Inbox { get; init; }

    public ChurchEvent? Event(string id) => Events.FirstOrDefault(e => e.Id == id);

    public static SampleContent Create() => new()
    {
        Services =
        [
            new(DayOfWeek.Sunday, new(9, 0), TimeSpan.FromMinutes(45), "LIFE Groups", false, "groups"),
            new(DayOfWeek.Sunday, new(10, 30), TimeSpan.FromMinutes(90), "Morning Worship", true),
            new(DayOfWeek.Sunday, new(18, 0), TimeSpan.FromMinutes(90), "Evening Worship", true),
            new(DayOfWeek.Wednesday, new(19, 0), TimeSpan.FromMinutes(90), "Bible Study & Prayer", false),
        ],
        SermonNotes = new Dictionary<string, SermonNotes>
        {
            ["2026-10-25-anchored-part-3-anchored-in-community"] = new(null,
            [
                new("Passive Impact & Active Impact", new OutlineItem("Passages", "1 Corinthians 3:9", "John 5:7", "Romans 12:2", "John 15:5", "Ephesians 3:11–17")),
                new("3. Community Is Love in Practice",
                    new OutlineItem("When we isolate ourselves, we aren't just pulling away from each other; we are pulling away from the body that Christ shed His blood to establish.", "Hebrews 10:25–26", "Hebrews 12:6")),
            ]),
            ["2026-10-25-what-god-is-doing-in-the-dark-part-2-grace-at-the-edge-of-the-pit"] = new("Job 33:1–33",
            [
                new("1. God Speaks When We Think He Is Silent",
                    new OutlineItem("God's silence may be our failure to perceive."),
                    new OutlineItem("God has now spoken supremely in His Son.", "Hebrews 1:1–2", "John 14:9"),
                    new OutlineItem("God opens the ear.", "Luke 24:45")),
                new("2. God Restrains What We Cannot See Ahead",
                    new OutlineItem("Some of God's greatest mercies are interruptions.", "Acts 16:6–7"),
                    new OutlineItem("God may restrain pride.", "2 Corinthians 12:7, 9"),
                    new OutlineItem("God sees the pit before we do.")),
                new("3. God Opens the Ear Through Pain",
                    new OutlineItem("Pain can expose what comfort concealed.", "Luke 22:32"),
                    new OutlineItem("Pain can make the soul teachable.", "Job 36:15", "Hebrews 12:11"),
                    new OutlineItem("Discipline isn't condemnation.", "Romans 8:1", "Hebrews 12:6"),
                    new OutlineItem("Not all suffering is correction for a specific sin.", "John 9:2–3")),
                new("4. God Provides a Messenger and a Ransom",
                    new OutlineItem("The messenger interprets God's way."),
                    new OutlineItem("The messenger helps the sufferer see God more accurately.", "Galatians 6:1"),
                    new OutlineItem("The messenger points beyond himself.", "John 1:29", "2 Corinthians 4:5"),
                    new OutlineItem("Grace speaks before the sufferer can rescue himself.", "Romans 5:6", "Mark 10:45", "1 Timothy 2:6")),
                new("God Restores the Redeemed to Light",
                    new OutlineItem("God's ransom produces restoration.", "Psalm 51:12", "1 Peter 1:8", "1 John 1:9", "Colossians 1:13", "John 8:12"),
                    new OutlineItem("God works repeatedly.", "2 Peter 3:9")),
            ]),
        },
        SpeakerPhotos = new Dictionary<string, string>
        {
            ["Phil Parry"] = Cdn + "05/27/09/04/03/4e60da5e-9b78-4293-9504-5145ea6716c2/Parry-P_SQUARE.jpg",
            ["Hernan Hullana"] = Cdn + "05/27/03/35/52/54f20347-e0a1-49fe-b0dc-6a37b78a6dd8/Hullana-H_SQUARE.jpg",
            ["Andrew Barns"] = Cdn + "05/27/14/18/56/1d40faee-e613-4277-b937-fd2fdc1546b5/Barns-A_SQUARE.jpg",
            ["Paul Haycock"] = Cdn + "06/08/06/26/47/b0767826-dbb7-4031-9bad-1a41ba5b3602/Haycock-P_SQUARE.jpg",
            ["Jabez Wells"] = Cdn + "06/08/12/49/06/24968839-0274-4454-85bf-5e8c01abc033/Wells-J_SQUARE.jpg",
        },
        Bulletin = new(
            new(2026, 10, 25),
            "Almighty God",
            "Job 33:4",
            new("Welcome to Good Shepherd",
            [
                "We're glad you're here today. To our church family, welcome home; to anyone joining us for the first time, we're thankful you chose to worship with us.",
                "This year we aren't centring our hearts on a slogan but on a Person. In the book of Job, God reveals Himself as the Almighty: sovereign over suffering, unsearchable in wisdom, yet purposeful in all He permits.",
            ],
            "Don't settle for second-hand knowledge of God. Seek Him personally, trust Him deeply and worship Him reverently.",
            "Pastor Hernan Hullana"),
            ["2026-10-25-anchored-part-3-anchored-in-community", "2026-10-25-what-god-is-doing-in-the-dark-part-2-grace-at-the-edge-of-the-pit"],
            [
                new(DayOfWeek.Sunday, [new("10:30am", "Morning Service"), new("6:00pm", "Evening Service")]),
                new(DayOfWeek.Wednesday, [new("7:00pm", "Bible Study & Prayer")]),
                new(DayOfWeek.Friday, [new("5:00am", "Men's Prayer"), new("9:30am", "Ladies' Prayer"), new("6:30pm", "Impact Kids"), new("6:30pm", "Impact Youth")]),
            ],
            [
                new("summer-fit", "8 – 29 Nov · 9am", "Summer FIT",
                    "As the LIFE Groups year draws to a close, all adult classes combine for a four-week study to prepare us spiritually for summer: to finish the year well, not drift through it.",
                    "See event", "/events/summer-fit"),
                new("baptisms", "Next step", "Baptisms",
                    "If you're interested in getting baptised, please speak to someone at the church.",
                    "Contact us", "/visit"),
            ]),
        Events =
        [
            new("summer-fit", "Summer FIT",
            [
                "As the LIFE Groups year draws to a close, all adult classes will combine for a four-week study designed to prepare us spiritually for the summer season.",
                "Summer FIT will help us enter the season intentionally: godly, unburdened, Spirit-strengthened and ready for the Master's use.",
            ],
                new(2026, 11, 8, 9, 0, 0), TimeSpan.FromMinutes(75), "Good Shepherd", EventCategory.Adults, null, "Sundays, 8 – 29 Nov"),
            new("bible-study", "Bible Study & Prayer", ["Sample description."],
                new(2026, 10, 28, 19, 0, 0), TimeSpan.FromMinutes(90), "Good Shepherd", EventCategory.Church, null, "Every Wednesday"),
            new("mens-prayer", "Men's Prayer", ["Sample description."],
                new(2026, 10, 30, 5, 0, 0), TimeSpan.FromMinutes(60), "Good Shepherd", EventCategory.Prayer, null, "Every Friday"),
            new("ladies-prayer", "Ladies' Prayer", ["Sample description."],
                new(2026, 10, 30, 9, 30, 0), TimeSpan.FromMinutes(90), "Good Shepherd", EventCategory.Prayer, null, "Every Friday"),
            new("impact-kids", "Impact Kids", ["Sample description."],
                new(2026, 10, 30, 18, 30, 0), TimeSpan.FromMinutes(120), "Good Shepherd", EventCategory.Kids, null, "Every Friday"),
            new("impact-youth", "Impact Youth", ["Sample description."],
                new(2026, 10, 30, 18, 30, 0), TimeSpan.FromMinutes(120), "Good Shepherd", EventCategory.Youth, null, "Every Friday"),
        ],
        PrayerWall =
        [
            new("Sample request: for everyone starting the Summer FIT study.", "2h", 14),
            new("Sample request: for our church family as the year winds down.", "Yesterday", 31),
            new("Sample request: for families facing illness this week.", "2 days", 22),
        ],
        Topics =
        [
            new("general", "General", "Announcements and service changes"),
            new("kids", "Kids", "Impact Kids news for parents"),
            new("youth", "Youth", "Impact Youth nights and camps"),
            new("young-adults", "Young Adults", "Gatherings and studies"),
            new("seniors", "Seniors", "Fellowship and outings"),
            new("prayer", "Prayer", "New requests on the prayer wall"),
        ],
        Inbox =
        [
            new("Music: you're on Sunday", "Choir item, morning service. Sample notification: tap to reply on the roster.", "Fri", true, "ministries/music/roster"),
            new("This week's bulletin is ready", "Sunday 25 October: Almighty God. Tap to read before the service.", "Sun", true, ""),
            new("Summer FIT starts 8 November", "All adult classes combine for four weeks. See you at 9am.", "Thu", false, "events/summer-fit"),
            new("Music team notice", "Team night, Saturday 14 November. Sample notification: tap to open the team's notices.", "Wed", false, "ministries/music/members"),
        ],
    };
}
