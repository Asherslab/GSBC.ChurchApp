using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal static class ChatCards
{
    public static ChatCard For(
        Published published, Participant you, IReadOnlyDictionary<Guid, ParticipantRecord> state, IReadOnlyDictionary<Guid, string> names, MessageRecord? last)
    {
        (ChatRecord chat, _, MembershipPlan plan, MembershipFacts facts) = published;
        bool leader = you.Role == ChatRoles.Leader;
        ParticipantRecord? yours = state.GetValueOrDefault(you.PersonId);
        return new ChatCard
        {
            Id = chat.Id.ToString(),
            MinistryId = chat.MinistryId.ToString(),
            MinistryName = published.Place?.Name ?? "",
            Name = chat.Name,
            Kind = chat.Kind,
            Audience = chat.Audience,
            YourRole = you.Role,
            YouMaySend = ChatMembership.MaySend(plan, chat.Kind, you.PersonId),
            YouMayManage = leader,
            ForChildren = you.ForChildren.Select(child => Person(child, names)).ToList(),
            ParentsCanRead = plan.HasMinors,
            Muted = yours?.Muted ?? false,
            LastSeq = chat.LastSeq,
            LastPreview = last is null ? "" : last.DeletedAt is null ? ChatRows.Snippet(last) : ChatCopy.DeletedMessage,
            LastAt = last?.SentAt,
            LastSender = last is null ? "" : ChatRows.NameOf(last.SenderId, names),
            ReadSeq = yours?.ReadSeq ?? 0,
            People = plan.Participants.Select(participant => new ChatPerson
            {
                Id = participant.PersonId.ToString(),
                Name = names.GetValueOrDefault(participant.PersonId, "Someone"),
                Role = participant.Role,
                State = participant.State,
                Minor = participant.Minor,
                BlueCard = leader && facts.BlueCards.Contains(participant.PersonId),
                ReadSeq = state.GetValueOrDefault(participant.PersonId)?.ReadSeq ?? 0,
                ParentOf = participant.ForChildren.Select(child => child.ToString()).ToList(),
            }).ToList(),
            AwaitingYourConsent = you.ForChildren
                .Where(child => plan.Of(child) is { State: ChatStates.AwaitingConsent })
                .Select(child => Person(child, names))
                .ToList(),
            Flags = leader ? plan.Flags.Select(flag => Flagged.Words(flag, names)).ToList() : [],
            CreatedAt = chat.CreatedAt,
            Asker = chat.AskerId is { } asker ? Person(asker, names) : null,
            HasMinors = plan.HasMinors,
            FirstSeq = 1,
            You = you.PersonId.ToString(),
            AskedAs = chat.AskedAs,
        };
    }

    private static PersonRef Person(Guid id, IReadOnlyDictionary<Guid, string> names) => new() { Id = id.ToString(), Name = names.GetValueOrDefault(id, "Someone") };
}
