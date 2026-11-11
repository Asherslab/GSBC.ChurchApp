using GSBC.ChurchApp.Features.Chats.Contracts;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed record PersonFacts(Guid Id, bool Minor, IReadOnlyList<Guid> Parents);

internal sealed record MembershipFacts(
    bool Open,
    bool LeadersJoin,
    IReadOnlyCollection<Guid> InAudience,
    IReadOnlyCollection<Guid> Leaders,
    IReadOnlyDictionary<Guid, PersonFacts> People,
    IReadOnlyDictionary<Guid, bool?> Consents,
    IReadOnlySet<Guid> Paused,
    IReadOnlySet<Guid> BlueCards);

internal sealed record Participant(Guid PersonId, string Role, string State, IReadOnlyList<Guid> ForChildren, bool Minor = false)
{
    public bool Receives => State == ChatStates.Active;

    public bool Writes => Receives && Role != ChatRoles.ParentReader;
}

internal sealed record MembershipPlan(IReadOnlyList<Participant> Participants, IReadOnlyList<string> Flags, bool HasMinors)
{
    public static readonly MembershipPlan Nobody = new([], [], false);

    public IEnumerable<Participant> Recipients => Participants.Where(participant => participant.Receives);

    public Participant? Of(Guid personId) => Participants.FirstOrDefault(participant => participant.PersonId == personId);
}

internal static class ChatMembership
{
    public const int AdultsNeeded = 2;

    public static MembershipPlan Decide(MembershipFacts facts)
    {
        if (!facts.Open)
            return MembershipPlan.Nobody;

        HashSet<Guid> leaders = facts.Leaders.Where(person => !facts.Paused.Contains(person)).ToHashSet();
        List<Guid> everyone = facts.InAudience.Concat(facts.LeadersJoin ? leaders : []).Where(person => !facts.Paused.Contains(person)).Distinct().ToList();
        Dictionary<Guid, Participant> participants = [];
        Dictionary<Guid, List<Guid>> childrenOfParent = [];
        List<string> flags = [];
        bool hasMinors = false;

        foreach (Guid person in everyone)
        {
            PersonFacts personFacts = Facts(facts, person);
            if (personFacts.Minor && !leaders.Contains(person))
            {
                hasMinors = true;
                bool approved = facts.Consents.GetValueOrDefault(person) == true;
                participants[person] = new Participant(person, ChatRoles.Member, approved ? ChatStates.Active : ChatStates.AwaitingConsent, [], Minor: true);
                List<Guid> parents = personFacts.Parents.Where(parent => !facts.Paused.Contains(parent)).ToList();
                if (parents.Count == 0)
                    flags.Add(Flagged.NoParent(person));
                foreach (Guid parent in parents)
                {
                    if (!childrenOfParent.TryGetValue(parent, out List<Guid>? children))
                        childrenOfParent[parent] = children = [];
                    children.Add(person);
                }
            }
            else
            {
                participants[person] = new Participant(person, leaders.Contains(person) ? ChatRoles.Leader : ChatRoles.Member, ChatStates.Active, []);
            }
        }

        foreach ((Guid parent, List<Guid> children) in childrenOfParent)
        {
            participants[parent] = participants.TryGetValue(parent, out Participant? already)
                ? already with { ForChildren = children }
                : new Participant(parent, ChatRoles.ParentReader, ChatStates.Active, children);
        }

        if (hasMinors)
        {
            int carded = participants.Values.Count(participant =>
                participant.Role != ChatRoles.ParentReader && !Facts(facts, participant.PersonId).Minor && facts.BlueCards.Contains(participant.PersonId));
            if (carded < AdultsNeeded)
                flags.Insert(0, Flagged.TooFewCardedAdults(carded));
        }

        return new MembershipPlan(participants.Values.OrderBy(participant => participant.PersonId).ToList(), flags, hasMinors);
    }

    public static bool MaySend(MembershipPlan plan, string kind, Guid sender) =>
        plan.Of(sender) is { } participant && participant.Writes && (kind != ChatKinds.Channel || participant.Role == ChatRoles.Leader);

    public static bool MaySendFor(MembershipPlan plan, string kind, Guid parent, Guid child) =>
        kind != ChatKinds.Channel
        && plan.Of(parent) is { Receives: true } reader && reader.ForChildren.Contains(child)
        && plan.Of(child) is { Receives: true };

    private static PersonFacts Facts(MembershipFacts facts, Guid person) =>
        facts.People.GetValueOrDefault(person) ?? new PersonFacts(person, false, []);
}

internal static class Flagged
{
    public const string NoParentPrefix = "no-parent:";
    public const string TooFewCardedPrefix = "too-few-carded:";

    public static string NoParent(Guid child) => NoParentPrefix + child.ToString("N");

    public static string TooFewCardedAdults(int carded) => TooFewCardedPrefix + carded;

    public static string Words(string flag, IReadOnlyDictionary<Guid, string> names)
    {
        if (flag.StartsWith(TooFewCardedPrefix, StringComparison.Ordinal))
        {
            int carded = int.Parse(flag[TooFewCardedPrefix.Length..], System.Globalization.CultureInfo.InvariantCulture);
            return carded switch
            {
                0 => "This chat has young people in it and no adult with a current blue card. Add two adults who have one.",
                _ => "This chat has young people in it and only one adult with a current blue card. Add another.",
            };
        }
        if (flag.StartsWith(NoParentPrefix, StringComparison.Ordinal) && Guid.TryParse(flag[NoParentPrefix.Length..], out Guid child))
            return $"{names.GetValueOrDefault(child, "A young person")} has no parent linked in the app, so they can't get this chat until one is.";
        return flag;
    }
}
