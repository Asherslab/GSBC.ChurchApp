using GSBC.ChurchApp.Features.Chats.Contracts;

namespace GSBC.ChurchApp.Features.Chats.Client;

public static class ChatMentions
{
    public static IEnumerable<(string Text, bool Mention, bool You)> Split(ChatMessage message, ChatCard card) => Split(message.Text, message, card);

    public static IEnumerable<(string Text, bool Mention, bool You)> Split(string text, ChatMessage message, ChatCard card)
    {
        List<(string Tag, string Id)> tags = message.Mentions
            .Select(id => (Person: card.People.FirstOrDefault(person => person.Id == id), Id: id))
            .Where(found => found.Person is not null)
            .Select(found => ($"@{found.Person!.Name}", found.Id))
            .OrderByDescending(tag => tag.Item1.Length)
            .ToList();
        int at = 0;
        while (at < text.Length)
        {
            (int index, string tag, string id) = tags
                .Select(tag => (Index: text.IndexOf(tag.Tag, at, StringComparison.Ordinal), tag.Tag, tag.Id))
                .Where(found => found.Index >= 0)
                .OrderBy(found => found.Index)
                .FirstOrDefault((-1, "", ""));
            if (index < 0)
            {
                yield return (text[at..], false, false);
                yield break;
            }
            if (index > at)
                yield return (text[at..index], false, false);
            yield return (tag, true, id == card.You);
            at = index + tag.Length;
        }
    }

    public static IReadOnlyList<string> Kept(string text, IReadOnlyDictionary<string, string> chosen) =>
        chosen.Where(mention => text.Contains($"@{mention.Value}", StringComparison.Ordinal)).Select(mention => mention.Key).ToList();
}
