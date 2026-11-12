using System.Globalization;
using GSBC.ChurchApp.Features.Chats.Client;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Chats;

namespace GSBC.ChurchApp.Features.Chats.Admin;

public static class ChatLead
{
    public static string Count(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";

    public static string Day(DateOnly day) => day.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture);

    public static string Of(DeskChat chat)
    {
        string kind = chat.Kind == ChatKinds.Ask ? "asking to join" : $"for {(chat.Audience == ChatAudiences.Team ? "the team" : ChatText.Audience(chat.Audience).ToLowerInvariant())}{(chat.Kind == ChatKinds.Channel ? ", leaders post" : "")}";
        return $"{chat.MinistryName} · {kind} · {Count(chat.People, "person", "people")} · {Count(chat.Messages, "message", "messages")}{(chat.HasMinors ? " · parents of young people can read it" : "")}";
    }
}
