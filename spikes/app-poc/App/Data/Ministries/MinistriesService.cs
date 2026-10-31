using Gsbc.AppPoc.Native;

namespace Gsbc.AppPoc.Data.Ministries;

public sealed class MinistriesService(ChurchClock clock, NativeBridge native)
{
    private const string RoleKey = "ministries:role";
    private const string JoinedKey = "ministries:joined";
    private const string StaffAuthor = "Church office";

    private readonly Dictionary<string, MinistrySettings> settings = MinistriesContent.All.ToDictionary(m => m.Id, m => new MinistrySettings(m, DefaultAudiences(m)));
    private readonly Dictionary<string, List<Person>> members = MinistriesContent.All.ToDictionary(m => m.Id, m => MinistriesContent.Members.GetValueOrDefault(m.Id)?.ToList() ?? []);
    private readonly Dictionary<string, List<Person>> requests = MinistriesContent.All.Where(m => m.Join == JoinMode.Ask).ToDictionary(m => m.Id, _ => MinistriesContent.AskedToJoin.ToList());
    private readonly List<MinistryPost> posts = [.. MinistriesContent.Posts(), .. LifeGroupsContent.Posts()];
    private readonly List<MinistryEvent> events = [.. MinistriesContent.Events(), .. LifeGroupsContent.Events()];
    private readonly List<MinistryFile> files = [.. MinistriesContent.Files(), .. LifeGroupsContent.Files()];
    private readonly List<MinistryChat> chats = [.. MinistriesContent.Chats()];
    private readonly List<RosterDate> rosters = [.. MinistriesContent.Rosters()];
    private readonly HashSet<string> joined = [];
    private readonly HashSet<string> left = [];
    private readonly HashSet<string> asked = [];
    private readonly HashSet<string> messaged = [];
    private readonly HashSet<string> muted = [];
    private Task? loading;
    private int chatCount;

    public ViewerRole Role { get; private set; } = ViewerRole.Member;

    public bool IsStaff => Role == ViewerRole.Staff;

    public DateTime Now => clock.Now;

    public event Action? Changed;

    public static IReadOnlyList<Ministry> All => MinistriesContent.All;

    public static IEnumerable<Ministry> TopLevel => OfKind(MinistryKind.Ministry).Where(m => m.ParentId is null);

    public static IEnumerable<Ministry> OfKind(MinistryKind kind) => All.Where(m => m.Kind == kind);

    public Task EnsureLoadedAsync() => loading ??= LoadAsync();

    public static Ministry? Find(string id) => All.FirstOrDefault(m => m.Id == id);

    public static Ministry? Parent(Ministry ministry) => ministry.ParentId is { } id ? Find(id) : null;

    public static IReadOnlyList<Ministry> Children(Ministry ministry) => All.Where(m => m.ParentId == ministry.Id).ToList();

    public MinistrySettings Settings(Ministry ministry) => settings[ministry.Id];

    public bool Has(Ministry ministry, MinistryModule module) => settings[ministry.Id].Modules.Contains(module);

    public IReadOnlyList<Person> Members(Ministry ministry) => members[ministry.Id];

    public IReadOnlyList<Person> Requests(Ministry ministry) => requests.GetValueOrDefault(ministry.Id) ?? [];

    public Relation RelationTo(Ministry ministry)
    {
        if (IsStaff || Leads(ministry) || (Parent(ministry) is { } parent && Leads(parent)))
        {
            return Relation.Leader;
        }
        return IsOwnMember(ministry) || Children(ministry).Any(IsOwnMember) ? Relation.Member : Relation.Visitor;
    }

    public bool IsMember(Ministry ministry) => RelationTo(ministry) >= Relation.Member;

    public bool IsLeader(Ministry ministry) => RelationTo(ministry) == Relation.Leader;

    public bool IsOwnMember(Ministry ministry) =>
        !left.Contains(ministry.Id) && (joined.Contains(ministry.Id) || MinistriesContent.MemberOf[Role].Contains(ministry.Id));

    public bool HasAsked(Ministry ministry) => asked.Contains(ministry.Id);

    public bool HasMessaged(Ministry ministry) => messaged.Contains(ministry.Id);

    public bool PushOn(Ministry ministry) => !muted.Contains(ministry.Id);

    public IReadOnlyList<Ministry> Mine =>
        OfKind(MinistryKind.Ministry).Where(m => IsOwnMember(m) || Leads(m)).ToList();

    public IEnumerable<MinistryPost> Posts(Ministry ministry) =>
        posts.Where(p => FromHere(ministry, p.MinistryId, MinistryModule.Announcements))
            .Select(p => p with { Audience = Effective(p.MinistryId, MinistryModule.Announcements, p.Audience) })
            .Where(p => CanSee(ministry, p.Audience))
            .OrderByDescending(p => p.Posted);

    public IEnumerable<MinistryEvent> Events(Ministry ministry, bool withPast = false) =>
        events.Where(e => FromHere(ministry, e.MinistryId, MinistryModule.Calendar) && (withPast || e.Start + e.Length > Now))
            .Select(e => e with { Audience = Effective(e.MinistryId, MinistryModule.Calendar, e.Audience) })
            .Where(e => CanSee(ministry, e.Audience))
            .OrderBy(e => e.Start);

    public IEnumerable<MinistryFile> Files(Ministry ministry) =>
        files.Where(f => FromHere(ministry, f.MinistryId, MinistryModule.Files))
            .Select(f => f with { Audience = Effective(f.MinistryId, MinistryModule.Files, f.Audience) })
            .Where(f => CanSee(ministry, f.Audience));

    public ModuleAudience AudienceOf(Ministry ministry, MinistryModule module) =>
        IsAlwaysMembers(module) ? ModuleAudience.Members : settings[ministry.Id].Audiences.GetValueOrDefault(module, ModuleAudience.Members);

    public IReadOnlyList<Audience> Allowed(Ministry ministry, MinistryModule module) => AudienceOf(ministry, module) switch
    {
        ModuleAudience.Everyone => [Audience.Everyone],
        ModuleAudience.Both => [Audience.Members, Audience.Everyone],
        _ => [Audience.Members],
    };

    public static bool IsAlwaysMembers(MinistryModule module) => module is MinistryModule.Chats or MinistryModule.Rosters;

    public static IReadOnlyList<MinistryModule> SharedModules => [MinistryModule.Announcements, MinistryModule.Calendar, MinistryModule.Files];

    public IReadOnlyList<MinistryModule> MembersOnlyModules(Ministry ministry) =>
        [.. Settings(ministry).Modules.Where(m => AudienceOf(ministry, m) != ModuleAudience.Everyone && (m != MinistryModule.Chats || ChatsAllowed(ministry))).Order()];

    public void SetAudience(Ministry ministry, MinistryModule module, ModuleAudience audience)
    {
        settings[ministry.Id].Audiences[module] = audience;
        Changed?.Invoke();
    }

    private Audience Effective(string sourceId, MinistryModule module, Audience posted) =>
        Find(sourceId) is { } source && AudienceOf(source, module) == ModuleAudience.Members ? Audience.Members : posted;

    private static Dictionary<MinistryModule, ModuleAudience> DefaultAudiences(Ministry ministry) =>
        SharedModules.ToDictionary(m => m, m => ministry.Audiences is { } chosen && chosen.TryGetValue(m, out var audience) ? audience : ministry switch
        {
            { Kind: MinistryKind.LifeGroup } => ModuleAudience.Everyone,
            { ServingTeam: true } => ModuleAudience.Members,
            _ when Parent(ministry) is { ServingTeam: true } => ModuleAudience.Members,
            _ => ModuleAudience.Everyone,
        });

    public IEnumerable<MinistryChat> Chats(Ministry ministry) =>
        IsMember(ministry)
            ? chats.Where(c => FromHere(ministry, c.MinistryId, MinistryModule.Chats))
            : [];

    public MinistryChat? Chat(string id) => chats.FirstOrDefault(c => c.Id == id);

    public bool ChatsAllowed(Ministry ministry) => !ministry.WithChildren && (Parent(ministry) is not { WithChildren: true });

    public bool CanStartChat(Ministry ministry) =>
        ChatsAllowed(ministry) && Has(ministry, MinistryModule.Chats) && (IsLeader(ministry) || (IsMember(ministry) && Settings(ministry).MembersStartChats));

    public bool CanWrite(MinistryChat chat) => Find(chat.MinistryId) is { } ministry && (chat.LeadersOnly ? IsLeader(ministry) : IsMember(ministry));

    public IReadOnlyList<RosterDate> Roster(Ministry ministry)
    {
        if (!IsMember(ministry))
        {
            return [];
        }
        var own = rosters.Where(r => r.MinistryId == ministry.Id && Has(ministry, MinistryModule.Rosters));
        var fromParent = rosters
            .Where(r => r.MinistryId == ministry.ParentId && Has(ministry, MinistryModule.Rosters))
            .Select(r => r with { Slots = r.Slots.Where(s => s.MinistryId == ministry.Id).ToList() })
            .Where(r => r.Slots.Count > 0);
        return own.Concat(fromParent).Where(r => r.Start.Date >= Now.Date).OrderBy(r => r.Start).ToList();
    }

    public static bool IsYou(Person person) => person == MinistriesContent.You;

    public IReadOnlyList<SlotOnRoster> YourSlots =>
        rosters.Where(r => r.Start.Date >= Now.Date)
            .SelectMany(r => r.Slots.Where(s => s.People.Any(IsYou) && s.YourReply != RosterReply.Declined).Select(s => (Date: r, Slot: s)))
            .Select(x => new SlotOnRoster(x.Date, x.Slot, Find(x.Slot.MinistryId)!))
            .Where(x => IsMember(x.Ministry))
            .OrderBy(x => x.Date.Start)
            .ToList();

    public IReadOnlyList<SlotOnRoster> YourSlotsIn(Ministry ministry) =>
        YourSlots.Where(s => s.Ministry == ministry || s.Date.MinistryId == ministry.Id).ToList();

    public HomeLine Home()
    {
        if (YourSlots.FirstOrDefault() is { } next)
        {
            var reply = next.Slot.YourReply == RosterReply.Waiting ? " · please reply" : "";
            return new($"You're on {clock.RelativeInSentence(next.Date.Start)}", $"{next.Slot.Position} · {next.Date.Service}, {ChurchClock.Time(next.Date.Start)}{reply}", $"ministries/{next.Date.MinistryId}/roster", next.Ministry.Colour, "roster");
        }
        if (Mine is { Count: > 0 } mine)
        {
            return new("Your ministries", Names(mine), "ministries", mine[0].Colour);
        }
        return new("Ministries", "Music, Kids, Youth and more: find where you fit", "ministries");
    }

    public static string Names(IReadOnlyList<Ministry> list) => list.Count switch
    {
        0 => "",
        1 => list[0].Name,
        _ => $"{string.Join(", ", list.Take(list.Count - 1).Select(m => m.Name))} and {list[^1].Name}",
    };

    public static string JoinLabel(JoinMode mode) => mode switch
    {
        JoinMode.Open => "Open to all",
        JoinMode.Ask => "Ask to join",
        _ => "By invitation",
    };

    public static string ModuleName(MinistryModule module) => module switch
    {
        MinistryModule.Calendar => "Calendar",
        MinistryModule.Announcements => "Announcements",
        MinistryModule.Chats => "Chats",
        MinistryModule.Rosters => "Rosters",
        _ => "Files",
    };

    public string Offers(Ministry ministry) =>
        string.Join(" · ", Settings(ministry).Modules.Order().Select(ModuleName));

    public static string Count(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    public string MembersPhrase(Ministry ministry) =>
        Children(ministry) is { Count: > 0 } children
            ? $"{ministry.Name} members, including {Names(children)}"
            : $"{ministry.Name} members";

    public async Task SetRoleAsync(ViewerRole role)
    {
        Role = role;
        await native.SetAsync(RoleKey, role.ToString());
        Changed?.Invoke();
    }

    public async Task JoinAsync(Ministry ministry)
    {
        left.Remove(ministry.Id);
        joined.Add(ministry.Id);
        if (!members[ministry.Id].Contains(MinistriesContent.You))
        {
            members[ministry.Id].Add(MinistriesContent.You);
        }
        await SaveAsync();
    }

    public async Task LeaveAsync(Ministry ministry)
    {
        joined.Remove(ministry.Id);
        left.Add(ministry.Id);
        members[ministry.Id].Remove(MinistriesContent.You);
        await SaveAsync();
    }

    public void AskToJoin(Ministry ministry)
    {
        asked.Add(ministry.Id);
        Changed?.Invoke();
    }

    public void MessageLeaders(Ministry ministry)
    {
        messaged.Add(ministry.Id);
        Changed?.Invoke();
    }

    public void SetPush(Ministry ministry, bool on)
    {
        if (on)
        {
            muted.Remove(ministry.Id);
        }
        else
        {
            muted.Add(ministry.Id);
        }
        Changed?.Invoke();
    }

    public void AnswerRequest(Ministry ministry, Person person, bool accept)
    {
        requests[ministry.Id].Remove(person);
        if (accept)
        {
            members[ministry.Id].Add(person);
        }
        Changed?.Invoke();
    }

    public void Post(Ministry ministry, string title, string body, Audience audience, bool important)
    {
        posts.Add(new(ministry.Id, title, body, Now, audience, $"{MinistriesContent.You.Name} (you)", important));
        Changed?.Invoke();
    }

    public void AddEvent(Ministry ministry, string title, DateTime start, string place, Audience audience)
    {
        events.Add(new(ministry.Id, title, start, TimeSpan.FromHours(2), place, audience));
        Changed?.Invoke();
    }

    public void AddLink(Ministry ministry, string title, string href, Audience audience)
    {
        files.Add(new(ministry.Id, title, "Link", FileKind.Link, audience, href));
        Changed?.Invoke();
    }

    public int SendToServingTeams(string title, string body)
    {
        var teams = TopLevel.Where(m => settings[m.Id].ServingTeam).ToList();
        foreach (var team in teams)
        {
            posts.Add(new(team.Id, title, body, Now, Audience.Members, StaffAuthor, Important: true, ToAllTeams: true));
        }
        Changed?.Invoke();
        return teams.Count;
    }

    public IReadOnlyList<Ministry> ServingTeams => OfKind(MinistryKind.Ministry).Where(m => settings[m.Id].ServingTeam || Parent(m) is { } p && settings[p.Id].ServingTeam).ToList();

    public MinistryChat StartChat(Ministry ministry, string name, bool leadersOnly)
    {
        var chat = new MinistryChat($"{ministry.Id}-new-{++chatCount}", ministry.Id, name, leadersOnly, []);
        chats.Add(chat);
        Changed?.Invoke();
        return chat;
    }

    public void Send(MinistryChat chat, string text)
    {
        chat.Messages.Add(new(MinistriesContent.You.Name, text, Now));
        Changed?.Invoke();
    }

    public void Reply(RosterSlot slot, RosterReply reply)
    {
        slot.YourReply = reply;
        Changed?.Invoke();
    }

    public void ToggleOnSlot(RosterSlot slot, Person person)
    {
        if (!slot.People.Remove(person))
        {
            slot.People.Add(person);
        }
        if (IsYou(person))
        {
            slot.YourReply = RosterReply.Waiting;
        }
        Changed?.Invoke();
    }

    public void SetModule(Ministry ministry, MinistryModule module, bool on)
    {
        var modules = settings[ministry.Id].Modules;
        if (on && (module != MinistryModule.Chats || ChatsAllowed(ministry)))
        {
            modules.Add(module);
        }
        else
        {
            modules.Remove(module);
        }
        Changed?.Invoke();
    }

    public void SetJoin(Ministry ministry, JoinMode mode)
    {
        settings[ministry.Id].Join = mode;
        Changed?.Invoke();
    }

    public void SetMembersStartChats(Ministry ministry, bool on)
    {
        settings[ministry.Id].MembersStartChats = on;
        Changed?.Invoke();
    }

    public void SetServingTeam(Ministry ministry, bool on)
    {
        settings[ministry.Id].ServingTeam = on;
        Changed?.Invoke();
    }

    private bool Leads(Ministry ministry) => MinistriesContent.Leads[Role].Contains(ministry.Id);

    private bool FromHere(Ministry ministry, string itemMinistryId, MinistryModule module) =>
        (itemMinistryId == ministry.Id || itemMinistryId == ministry.ParentId) && Find(itemMinistryId) is { } source && Has(source, module) && Has(ministry, module);

    private bool CanSee(Ministry ministry, Audience audience) => audience == Audience.Everyone || IsMember(ministry);

    private async Task LoadAsync()
    {
        if (Enum.TryParse<ViewerRole>(await native.GetAsync(RoleKey), out var role))
        {
            Role = role;
        }
        foreach (var id in (await native.GetAsync(JoinedKey))?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [])
        {
            joined.Add(id);
        }
        Changed?.Invoke();
    }

    private async Task SaveAsync()
    {
        await native.SetAsync(JoinedKey, string.Join(',', joined));
        Changed?.Invoke();
    }
}
