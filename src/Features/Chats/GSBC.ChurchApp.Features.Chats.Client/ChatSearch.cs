using GSBC.ChurchApp.Features.Chats.Contracts;

namespace GSBC.ChurchApp.Features.Chats.Client;

public static class ChatSearch
{
    public static bool Matches(ChatCard card, string search)
    {
        string[] words = search.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
            return true;
        string[] places = [ChatText.Title(card), card.MinistryName, card.LastPreview, .. card.People.Select(person => person.Name)];
        return words.All(word => places.Any(place => place.Contains(word, StringComparison.OrdinalIgnoreCase)));
    }
}
