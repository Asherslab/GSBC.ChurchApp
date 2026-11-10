using GSBC.ChurchApp.Platform.Contracts.Chats;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Chats.Client;

public static class ChatText
{
    public static string Audience(string audience) => audience switch
    {
        ChatAudiences.Team => "Team",
        ChatAudiences.Attendees => "Attendees and their parents",
        ChatAudiences.Parents => "Parents",
        _ => "Chosen people",
    };

    public static string Icon(ChatCard card) => card.Kind switch
    {
        ChatKinds.Channel => "megaphone",
        ChatKinds.Ask => "door-open",
        _ => "message-square",
    };

    public static RowTone Tone(ChatCard card) => card.Kind == ChatKinds.Ask ? RowTone.Quiet : card.Audience switch
    {
        ChatAudiences.Attendees => RowTone.Accent,
        ChatAudiences.Parents => RowTone.Warm,
        _ => RowTone.Primary,
    };

    public static string Title(ChatCard card) =>
        card.Kind == ChatKinds.Ask && card.Asker?.Id == card.You ? $"Asking to join {card.MinistryName}" : card.Name;

    public static string Preview(ChatCard card)
    {
        if (card.LastPreview.Length == 0)
            return card.YouMayManage ? "No messages yet. Say why you started it." : "No messages yet.";
        string sender = card.LastSender.Split(' ')[0];
        return $"{sender}: {card.LastPreview}";
    }

    public static string Who(ChatCard card)
    {
        int people = card.People.Count(person => person.Role != ChatRoles.ParentReader && person.State == ChatStates.Active);
        string count = people == 1 ? "1 person" : $"{people} people";
        if (card.YourRole == ChatRoles.ParentReader && card.ForChildren.Count > 0)
            return $"{First(card.ForChildren[0].Name)}'s chat · you can read it";
        return card.Kind switch
        {
            ChatKinds.Channel => $"Leaders post · {count}",
            ChatKinds.Ask => $"With {card.MinistryName}'s leaders",
            _ => $"{Audience(card.Audience)} · {count}",
        };
    }

    public static string When(DateTime utc, DateTime nowUtc)
    {
        DateTime local = ChurchTime.ToChurch(utc);
        DateTime today = ChurchTime.ToChurch(nowUtc).Date;
        return (today - local.Date).Days switch
        {
            0 => ChurchDates.Time(local),
            1 => "Yesterday",
            < 7 => local.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture),
            _ => local.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    public static string Day(DateTime utc, DateTime nowUtc)
    {
        DateTime local = ChurchTime.ToChurch(utc);
        DateTime today = ChurchTime.ToChurch(nowUtc).Date;
        return (today - local.Date).Days switch
        {
            0 => "Today",
            1 => "Yesterday",
            _ => local.ToString("dddd d MMMM", System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    public static string First(string name) => name.Split(' ', 2)[0];
}
