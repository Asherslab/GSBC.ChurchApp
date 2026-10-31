namespace Gsbc.AppPoc.Data.Ministries;

public static class MinistriesContent
{
    public const string Elvanto = "Elvanto";

    public static readonly Person You = new("Sam Sample");

    private static readonly Person Alex = new("Alex Sample");
    private static readonly Person Beth = new("Beth Example");
    private static readonly Person Chris = new("Chris Placeholder");
    private static readonly Person Dana = new("Dana Sample");
    private static readonly Person Eli = new("Eli Example");
    private static readonly Person Fran = new("Fran Placeholder");
    private static readonly Person Gus = new("Gus Sample");
    private static readonly Person Hana = new("Hana Example");
    private static readonly Person Ivy = new("Ivy Placeholder");
    private static readonly Person Jon = new("Jon Sample");
    private static readonly Person Kim = new("Kim Example");
    private static readonly Person Lou = new("Lou Placeholder");
    private static readonly Person Max = new("Max Sample");
    private static readonly Person Nia = new("Nia Example");
    private static readonly Person Oli = new("Oli Placeholder");
    private static readonly Person Pia = new("Pia Sample");

    private const string MusicColour = "#5e7dbe";

    private static readonly MinistryModule[] Everything =
        [MinistryModule.Calendar, MinistryModule.Announcements, MinistryModule.Chats, MinistryModule.Rosters, MinistryModule.Files];

    public static readonly IReadOnlyList<Ministry> Directory =
    [
        new("music", "Music", "Leads the singing at both Sunday services",
            ["Music leads the congregation in song at the morning and evening services, and looks after the four teams under it: Vocal Groups, Choir, Song Leading and Production.",
             "Sample description. Music's leaders also lead its four teams."],
            "Rehearsal Thursdays 7:30pm, Auditorium (sample)", [Alex, Beth], MusicColour, "music", JoinMode.Ask, true, Everything),
        new("vocal-groups", "Vocal Groups", "Small groups who sing a special item",
            ["Sample description. Trios and quartets who prepare a special item for a service, about once a month each."],
            "As rostered, rehearsal the Thursday before", [Chris], MusicColour, "mic", JoinMode.Ask, true,
            [MinistryModule.Calendar, MinistryModule.Announcements, MinistryModule.Chats, MinistryModule.Rosters, MinistryModule.Files], ParentId: "music"),
        new("choir", "Choir", "Sings at the morning service and at Christmas",
            ["Sample description. The church choir sings at the morning service once a month and leads the Christmas cantata. No audition: if you can hold a tune, come along."],
            "Thursdays 7:30pm, Auditorium (sample)", [Dana], MusicColour, "users", JoinMode.Ask, true, Everything, ParentId: "music"),
        new("song-leading", "Song Leading", "Leads the congregation from the front",
            ["Sample description. Song leaders choose and lead the congregational songs. Leaders invite new song leaders after they have served in Choir or Vocal Groups."],
            null, [Alex], MusicColour, "megaphone", JoinMode.InviteOnly, true,
            [MinistryModule.Announcements, MinistryModule.Rosters, MinistryModule.Files], ParentId: "music"),
        new("production", "Production", "Sound, projection and the livestream",
            ["Sample description. The production team runs the sound desk, the song words on screen and the YouTube livestream at both services. Training given."],
            "Sundays from 9:45am and 5:15pm", [Eli], MusicColour, "volume", JoinMode.Ask, true,
            [MinistryModule.Announcements, MinistryModule.Chats, MinistryModule.Rosters, MinistryModule.Files], ParentId: "music"),

        new("children", "Children's Ministry", "For children aged 4 to 11",
            ["Children ages 4 to 11.",
             "Sample description. On Friday nights in school term, Impact Kids has games, a Bible lesson and craft. Parents see the calendar and announcements here; the team's own notices and rosters are for its leaders and helpers."],
            "Impact Kids, Fridays 6:30pm in term", [Fran, Gus], "#f89833", "smile", JoinMode.Ask, true,
            [MinistryModule.Calendar, MinistryModule.Announcements, MinistryModule.Rosters, MinistryModule.Files], WhoFor: "Ages 4 – 11", WithChildren: true,
            Audiences: new Dictionary<MinistryModule, ModuleAudience> { [MinistryModule.Calendar] = ModuleAudience.Both, [MinistryModule.Announcements] = ModuleAudience.Both }),
        new("impact-youth", "Impact Youth", "Friday nights for high schoolers",
            ["Impact Youth is for all high school aged youth and is a great way to get to know other teens, discover what a genuine faith is all about, and an opportunity to have fun at the same time! We meet every Friday night during school terms from 6:30-8:30pm."],
            "Fridays 6:30 – 8:30pm in school terms", [Hana], "#6bcbd5", "zap", JoinMode.Ask, true,
            [MinistryModule.Calendar, MinistryModule.Announcements, MinistryModule.Rosters, MinistryModule.Files], WhoFor: "High school", WithChildren: true,
            Audiences: new Dictionary<MinistryModule, ModuleAudience> { [MinistryModule.Calendar] = ModuleAudience.Both, [MinistryModule.Announcements] = ModuleAudience.Both, [MinistryModule.Files] = ModuleAudience.Both }),
        new("southland-productions", "Southland Baptist Productions", "Named on the church's team page; what it does is to confirm",
            ["Sample description. Southland Baptist Productions is named on the church's Our Team page. What it does, and how it relates to Music's Production team, is still to confirm."],
            null, [Max], "#8e9aaf", "video", JoinMode.Ask, true,
            [MinistryModule.Announcements, MinistryModule.Files]),

        new("young-adults", "Young Adults", "For men and women 18 or older",
            ["Men & Women 18 or older.",
             "Sample description. Young adults meet for a Bible study and a meal, and for the odd games night or weekend away. Just turn up, or tap Join to see what's on."],
            "Tuesdays 7pm (sample)", [Ivy, Jon], "#5ec095", "coffee", JoinMode.Open, false,
            [MinistryModule.Calendar, MinistryModule.Announcements, MinistryModule.Chats, MinistryModule.Files], WhoFor: "18 and over", MembersStartChats: true,
            Audiences: new Dictionary<MinistryModule, ModuleAudience> { [MinistryModule.Announcements] = ModuleAudience.Both }),
        new("mens", "Men's", "Prayer and breakfasts for the men of the church",
            ["Sample description. Men's Prayer meets early every Friday, and a men's breakfast runs once a term."],
            "Men's Prayer, Fridays 5am", [Kim], "#8e769f", "users", JoinMode.Open, false,
            [MinistryModule.Calendar, MinistryModule.Announcements]),
        new("ladies", "Ladies", "Prayer, study and fellowship for women",
            ["Sample description. Ladies' Prayer meets every Friday morning, with a ladies' day each year."],
            "Ladies' Prayer, Fridays 9:30am", [Lou], "#f38fb3", "prayer", JoinMode.Open, false,
            [MinistryModule.Calendar, MinistryModule.Announcements]),
    ];

    public static readonly IReadOnlyDictionary<ViewerRole, IReadOnlyList<string>> MemberOf = new Dictionary<ViewerRole, IReadOnlyList<string>>
    {
        [ViewerRole.Visitor] = [],
        [ViewerRole.Member] = ["choir", "young-adults", "children"],
        [ViewerRole.Leader] = ["young-adults"],
        [ViewerRole.Staff] = [],
    };

    public static readonly IReadOnlyList<Ministry> All = [.. Directory, .. LifeGroupsContent.Groups];

    public static readonly IReadOnlyDictionary<ViewerRole, IReadOnlyList<string>> Leads = new Dictionary<ViewerRole, IReadOnlyList<string>>
    {
        [ViewerRole.Visitor] = [],
        [ViewerRole.Member] = [],
        [ViewerRole.Leader] = ["music", "children"],
        [ViewerRole.Staff] = [],
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<Person>> Members = new Dictionary<string, IReadOnlyList<Person>>
    {
        ["music"] = [Alex, Beth, Pia],
        ["vocal-groups"] = [Chris, Kim, Nia, Oli],
        ["choir"] = [Dana, You, Beth, Hana, Kim, Lou, Max, Nia],
        ["song-leading"] = [Alex, Beth, Jon],
        ["production"] = [Eli, Gus, Oli, Pia],
        ["children"] = [Fran, Gus, You, Ivy, Nia],
        ["impact-youth"] = [Hana, Jon, Max],
        ["young-adults"] = [Ivy, Jon, You, Oli, Pia],
        ["mens"] = [Kim, Gus, Max, Eli],
        ["ladies"] = [Lou, Dana, Nia, Hana],
        ["southland-productions"] = [Max, Eli],
    };

    public static readonly IReadOnlyList<Person> AskedToJoin = [Ivy, Oli];

    private static DateTime At(int month, int day, int hour, int minute = 0) => new(2026, month, day, hour, minute, 0);

    public static IEnumerable<MinistryPost> Posts() =>
    [
        new("music", "Christmas music starts next week", "Sample announcement. From 8 November we teach the morning service one Christmas song a week. Song leaders, the order is on the roster.",
            At(10, 28, 9), Audience.Members, "Alex Sample"),
        new("music", "Team night, Saturday 14 November", "Sample announcement. All four Music teams: dinner, a short devotion and planning for 2027. Partners welcome. Reply in the Music team chat so we can count chairs.",
            At(10, 29, 19), Audience.Members, "Beth Example", Important: true),
        new("choir", "Cantata scores are in the folder", "Sample announcement. Collect a printed copy on Sunday, or use the PDF in Files. Altos, please look at page 6 before Thursday.",
            At(10, 27, 20), Audience.Members, "Dana Sample"),
        new("choir", "Bring a friend to rehearsal", "Sample announcement. Anyone who'd like to sing in the cantata can come along on Thursday 5 November. Let Dana know who's coming.",
            At(10, 22, 10), Audience.Members, "Dana Sample"),
        new("production", "New sound desk training", "Sample announcement. Two sessions after the evening service in November. Ask in the Sound desk chat.",
            At(10, 26, 18), Audience.Members, "Eli Example"),
        new("children", "Last night of term: 4 December", "Sample announcement. Parents are invited to stay for the break-up night: a short presentation and supper from 8pm.",
            At(10, 28, 15), Audience.Everyone, "Fran Placeholder"),
        new("children", "This Friday: space night", "Sample announcement. Impact Kids goes to the stars this week. Dress up if you like; doors open at 6:15pm.",
            At(10, 30, 9), Audience.Everyone, "Gus Sample"),
        new("children", "Helpers: arrive by 6pm this Friday", "Sample announcement. Space night needs the hall set up early. Fran will brief everyone at 6:05pm.",
            At(10, 30, 9, 30), Audience.Members, "Fran Placeholder"),
        new("children", "Helpers: Working with Children cards", "Sample announcement. Please check your blue card is current before the end of term. The office needs a copy of any renewed card.",
            At(10, 26, 11), Audience.Members, "Gus Sample", Important: true),
        new("impact-youth", "Youth camp info night", "Sample announcement. Parents and youth: information about January's camp after Impact Youth on 13 November.",
            At(10, 25, 12), Audience.Everyone, "Hana Example"),
        new("impact-youth", "Leaders: camp rooms", "Sample announcement. Room lists for January's camp are in Files. Check your group before Friday.",
            At(10, 27, 20), Audience.Members, "Hana Example"),
        new("young-adults", "Games night this Saturday", "Sample announcement. Bring a board game and something to share. Everyone 18 and over welcome; bring a friend.",
            At(10, 28, 18), Audience.Everyone, "Ivy Placeholder"),
        new("young-adults", "Camp carpool", "Sample announcement. Drivers and riders are sorting it out in the Camp carpool chat.",
            At(10, 23, 14), Audience.Members, "Jon Sample"),
        new("mens", "Men's breakfast, 21 November", "Sample announcement. 7am in the hall, with a short talk. Bring a mate.",
            At(10, 24, 7), Audience.Everyone, "Kim Example"),
    ];

    public static IEnumerable<MinistryEvent> Events() =>
    [
        new("music", "Christmas cantata call", At(12, 13, 17, 15), TimeSpan.FromMinutes(45), "Auditorium", Audience.Members, "Choir and Vocal Groups, before the 6pm cantata. Sample event."),
        new("music", "Music team night", At(11, 14, 18), TimeSpan.FromHours(3), "Hall", Audience.Members, "All four teams. Sample event."),
        new("choir", "Choir rehearsal", At(11, 5, 19, 30), TimeSpan.FromMinutes(90), "Auditorium", Audience.Members, "Cantata pages 1 – 8. Sample event."),
        new("vocal-groups", "Vocal Groups rehearsal", At(11, 12, 19, 30), TimeSpan.FromHours(1), "Music room", Audience.Members, "Sample event."),
        new("children", "Impact Kids", At(11, 6, 18, 30), TimeSpan.FromHours(2), "Prep centre", Audience.Everyone, "Every Friday in term."),
        new("children", "Leaders' planning", At(11, 7, 9), TimeSpan.FromHours(2), "Prep centre", Audience.Members, "Term 1 planning. Sample event."),
        new("children", "Helpers' training night", At(11, 18, 19), TimeSpan.FromMinutes(90), "Prep centre", Audience.Members, "Check-in, allergies and Safe Ministry refresher. Sample event."),
        new("children", "Break-up night", At(12, 4, 18, 30), TimeSpan.FromHours(2), "Prep centre", Audience.Everyone, "Parents invited from 8pm. Sample event."),
        new("impact-youth", "Impact Youth", At(11, 6, 18, 30), TimeSpan.FromHours(2), "Youth building", Audience.Everyone, "Every Friday in term."),
        new("impact-youth", "Camp info night", At(11, 13, 20, 30), TimeSpan.FromMinutes(45), "Youth building", Audience.Everyone, "Sample event."),
        new("impact-youth", "Leaders' camp planning", At(11, 10, 19), TimeSpan.FromHours(2), "Youth building", Audience.Members, "Sample event."),
        new("young-adults", "Games night", At(10, 31, 19), TimeSpan.FromHours(3), "Hall", Audience.Everyone, "Sample event."),
        new("young-adults", "Bible study", At(11, 3, 19), TimeSpan.FromMinutes(90), "Library", Audience.Everyone, "Every Tuesday. Sample event."),
        new("mens", "Men's Prayer", At(11, 6, 5), TimeSpan.FromHours(1), "Good Shepherd", Audience.Everyone, "Every Friday."),
        new("mens", "Men's breakfast", At(11, 21, 7), TimeSpan.FromMinutes(90), "Hall", Audience.Everyone, "Sample event."),
        new("ladies", "Ladies' Prayer", At(11, 6, 9, 30), TimeSpan.FromMinutes(90), "Good Shepherd", Audience.Everyone, "Every Friday."),
    ];

    public static IEnumerable<MinistryFile> Files() =>
    [
        new("music", "Music team code of conduct", "PDF · 2 pages", FileKind.Document, Audience.Members),
        new("music", "2026 song list", "PDF · every song we sing, with keys", FileKind.Document, Audience.Members),
        new("music", "Practice recordings", "Dropbox folder · every song this term", FileKind.Link, Audience.Members, "https://www.dropbox.com/"),
        new("choir", "Christmas cantata score", "PDF · 24 pages", FileKind.Document, Audience.Members),
        new("production", "Sound desk checklist", "PDF · 1 page", FileKind.Document, Audience.Members),
        new("children", "Safe Ministry code of conduct", "PDF · what every leader agrees to", FileKind.Document, Audience.Members),
        new("children", "Term 4 program", "PDF · lessons and dates", FileKind.Document, Audience.Members),
        new("children", "Helpers' guide", "PDF · check-in, allergies, pick-up", FileKind.Document, Audience.Members),
        new("impact-youth", "Camp permission form", "PDF · due 4 December", FileKind.Document, Audience.Everyone),
        new("impact-youth", "Camp room lists", "PDF · leaders only", FileKind.Document, Audience.Members),
        new("young-adults", "Study notes: Philippians", "Google Docs link", FileKind.Link, Audience.Everyone, "https://docs.google.com/"),
    ];

    public static IEnumerable<MinistryChat> Chats() =>
    [
        new("music-team", "music", "Music team", false,
        [
            new("Beth Example", "Team night is on the 14th. Who's coming? Partners welcome.", At(10, 29, 19, 5)),
            new("Pia Sample", "Two of us.", At(10, 29, 19, 40)),
            new("Kim Example", "Count me in, I'll bring a salad.", At(10, 30, 7, 12)),
        ]),
        new("music-sunday", "music", "Sunday notices", true,
        [
            new("Alex Sample", "1 Nov: songs are up on the roster with practice recordings. Choir sings Amazing Grace in the morning.", At(10, 28, 21)),
        ]),
        new("choir-chat", "choir", "Choir", false,
        [
            new("Dana Sample", "Rehearsal Thursday, pages 1 – 8 of the cantata.", At(10, 27, 20, 10)),
            new("Hana Example", "Will there be a recording of the alto line?", At(10, 27, 21, 2)),
            new("Dana Sample", "Yes, in the Practice recordings folder by Wednesday.", At(10, 28, 8, 30)),
        ]),
        new("choir-cantata", "choir", "Christmas cantata", true,
        [
            new("Dana Sample", "Dress rehearsal Saturday 12 December, 2pm. Black and white, please.", At(10, 25, 16)),
        ]),
        new("production-desk", "production", "Sound desk", false,
        [
            new("Eli Example", "The new desk arrives on the 9th. Training after the evening service on the 15th and 22nd.", At(10, 26, 18, 20)),
            new("Oli Placeholder", "I'll do the 15th.", At(10, 26, 19, 1)),
        ]),
        new("vocal-chat", "vocal-groups", "Vocal Groups", false,
        [
            new("Chris Placeholder", "Trio for 15 Nov: Kim, Nia and Oli. Rehearsal on the 12th.", At(10, 28, 12)),
        ]),
        new("ya-chat", "young-adults", "Young Adults", false,
        [
            new("Ivy Placeholder", "Games night Saturday at 7! Bring snacks.", At(10, 28, 18, 30)),
            new("Jon Sample", "I'll bring Codenames.", At(10, 28, 19)),
        ]),
        new("ya-carpool", "young-adults", "Camp carpool", false,
        [
            new("Oli Placeholder", "Driving up Friday afternoon, three seats free.", At(10, 23, 13)),
        ]),
    ];

    private static readonly MinistryFile NewBritainRecording =
        new("music", "Choir parts, all voices", "Sample recording · 0:33", FileKind.Recording, Audience.Members, AudioPath: "audio/practice-new-britain.m4a");

    private static Song AmazingGrace() => new("Amazing Grace", "Key of G · tune New Britain · choir item",
    [
        NewBritainRecording,
        new("music", "Dropbox: full arrangement", "Sample link", FileKind.Link, Audience.Members, "https://www.dropbox.com/"),
        new("music", "Choir score", "PDF · 3 pages", FileKind.Document, Audience.Members),
    ]);

    private static Song Song(string title, string detail) => new(title, detail,
    [
        new("music", "Dropbox: practice recording", "Sample link", FileKind.Link, Audience.Members, "https://www.dropbox.com/"),
        new("music", "Chord chart", "PDF · 1 page", FileKind.Document, Audience.Members),
    ]);

    public static IEnumerable<RosterDate> Rosters() =>
    [
        new("music", At(11, 1, 10, 30), "Morning service",
        [
            new("Song leader", "song-leading", [Alex]),
            new("Piano", "music", [Pia]),
            new("Choir item", "choir", [Dana, You, Beth, Hana, Lou], RosterReply.Accepted),
            new("Sound", "production", [Eli]),
            new("Projection and livestream", "production", [Gus]),
        ],
        [AmazingGrace(), Song("Great Is Thy Faithfulness", "Key of D · congregation"), Song("Be Thou My Vision", "Key of E♭ · congregation")]),
        new("music", At(11, 8, 10, 30), "Morning service",
        [
            new("Song leader", "song-leading", [Beth]),
            new("Piano", "music", [Pia]),
            new("Vocal group", "vocal-groups", [Chris, Nia]),
            new("Sound", "production", [Oli]),
            new("Projection and livestream", "production", []),
        ],
        [Song("O Come, All Ye Faithful", "Key of G · congregation"), Song("It Is Well with My Soul", "Key of C · vocal group")]),
        new("music", At(11, 15, 10, 30), "Morning service",
        [
            new("Song leader", "song-leading", [Jon]),
            new("Piano", "music", [Pia]),
            new("Choir item", "choir", [Dana, You, Kim, Max, Nia]),
            new("Sound", "production", [Eli, Oli]),
            new("Projection and livestream", "production", [Gus]),
        ],
        [Song("Joy to the World", "Key of D · choir and congregation"), Song("Crown Him with Many Crowns", "Key of D · congregation")]),
        new("children", At(11, 6, 18, 30), "Impact Kids",
        [
            new("Lesson", "children", [Fran]),
            new("Games", "children", [Gus]),
            new("Helper", "children", [You, Ivy]),
            new("Check-in desk", "children", [Nia]),
        ], []),
        new("children", At(11, 13, 18, 30), "Impact Kids",
        [
            new("Lesson", "children", [Gus]),
            new("Games", "children", [Ivy]),
            new("Helper", "children", [Nia]),
            new("Check-in desk", "children", [You]),
        ], []),
        new("impact-youth", At(11, 6, 18, 30), "Impact Youth",
        [
            new("Talk", "impact-youth", [Hana]),
            new("Small group leaders", "impact-youth", [Jon, Max]),
        ], []),
    ];
}
