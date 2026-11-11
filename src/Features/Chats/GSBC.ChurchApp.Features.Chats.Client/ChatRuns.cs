using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Chats.Client;

public static class ChatRuns
{
    public static IEnumerable<(ChatLine Line, bool NewDay, bool First)> Of(IReadOnlyList<ChatLine> lines)
    {
        DateTime? lastDay = null;
        ChatMessage? previous = null;
        foreach (ChatLine line in lines)
        {
            ChatMessage message = line.Message;
            DateTime day = ChurchTime.ToChurch(message.SentAt).Date;
            bool newDay = lastDay != day;
            lastDay = day;
            bool first = newDay || previous is null || previous.Kind != MessageKinds.Text
                         || previous.SenderId != message.SenderId || previous.ForName != message.ForName;
            previous = message;
            yield return (line, newDay, first);
        }
    }
}
