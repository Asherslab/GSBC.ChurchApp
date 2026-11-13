using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Chats.Contracts;

public static class ReviewOnly
{
    public const string Everything = "";
    public const string Files = "files";
    public const string Changed = "changed";
    public const string Reported = "reported";
}

public sealed record ReviewFilter(string Words = "", string Who = "", DateOnly? From = null, DateOnly? To = null, string Only = ReviewOnly.Everything)
{
    public bool Narrows => Words.Trim().Length > 0 || Who.Length > 0 || From is not null || To is not null || Only != ReviewOnly.Everything;
}

public abstract record ReviewLine;

public sealed record ReviewDay(DateOnly Day) : ReviewLine;

public sealed record ReviewMessage(CheckedMessage Found, bool Match) : ReviewLine;

public sealed record ReviewFold(IReadOnlyList<string> Ids, bool Before, bool After) : ReviewLine;

public static class ChatReview
{
    private const int Around = 1;
    public const int Recent = 30;

    public static bool Matches(CheckedMessage found, ReviewFilter filter, IReadOnlySet<string> reported, Func<DateTime, DateOnly> dayOf)
    {
        ChatMessage message = found.Message;
        if (message.Kind == MessageKinds.Event)
            return false;
        DateOnly day = dayOf(message.SentAt);
        return (filter.Who.Length == 0 || message.SenderId == filter.Who)
               && (filter.From is not { } from || day >= from)
               && (filter.To is not { } to || day <= to)
               && Only(found, filter.Only, reported)
               && Says(found, filter.Words.Trim());
    }

    public static bool Only(CheckedMessage found, string only, IReadOnlySet<string> reported) => only switch
    {
        ReviewOnly.Files => found.Message.Attachments.Count > 0 || found.Versions.Any(version => version.Attachments.Count > 0),
        ReviewOnly.Changed => found.Message.EditedAt is not null || found.Message.Deleted,
        ReviewOnly.Reported => reported.Contains(found.Message.Id),
        _ => true,
    };

    public static IReadOnlyList<ReviewLine> Lines(IReadOnlyList<CheckedMessage> messages, ReviewFilter filter, IReadOnlySet<string> reported,
        IReadOnlySet<string> opened, Func<DateTime, DateOnly> dayOf)
    {
        bool[] match = messages.Select(found => filter.Narrows && Matches(found, filter, reported, dayOf)).ToArray();
        bool[] shown = new bool[messages.Count];
        for (int at = 0; at < messages.Count; at++)
        {
            if (!filter.Narrows && at >= messages.Count - Recent || opened.Contains(messages[at].Message.Id))
                shown[at] = true;
            if (!match[at])
                continue;
            for (int near = Math.Max(0, at - Around); near <= Math.Min(messages.Count - 1, at + Around); near++)
                shown[near] = true;
        }

        List<ReviewLine> lines = [];
        DateOnly? lastDay = null;
        List<string> folded = [];
        for (int at = 0; at < messages.Count; at++)
        {
            if (!shown[at])
            {
                folded.Add(messages[at].Message.Id);
                continue;
            }
            if (folded.Count > 0)
            {
                lines.Add(new ReviewFold(folded, Before: lines.Count == 0, After: false));
                folded = [];
            }
            DateOnly day = dayOf(messages[at].Message.SentAt);
            if (day != lastDay)
            {
                lines.Add(new ReviewDay(day));
                lastDay = day;
            }
            lines.Add(new ReviewMessage(messages[at], match[at]));
        }
        if (folded.Count > 0)
            lines.Add(new ReviewFold(folded, Before: lines.Count == 0, After: lines.Count > 0));
        return lines;
    }

    public static IReadOnlyList<(string Text, bool Found)> Marks(string text, string words)
    {
        string wanted = words.Trim();
        if (wanted.Length == 0)
            return [(text, false)];
        List<(string, bool)> parts = [];
        int start = 0;
        for (int at = text.IndexOf(wanted, StringComparison.OrdinalIgnoreCase); at >= 0; at = text.IndexOf(wanted, start, StringComparison.OrdinalIgnoreCase))
        {
            if (at > start)
                parts.Add((text[start..at], false));
            parts.Add((text.Substring(at, wanted.Length), true));
            start = at + wanted.Length;
        }
        if (start < text.Length)
            parts.Add((text[start..], false));
        return parts;
    }

    private static bool Says(CheckedMessage found, string words) =>
        words.Length == 0
        || Contains(found.Message.Text, words)
        || Contains(found.DeletedText, words)
        || found.Versions.Any(version => Contains(version.Text, words))
        || found.Message.Attachments.Any(attachment => Contains(attachment.Name, words));

    private static bool Contains(string text, string words) =>
        text.Contains(words, StringComparison.OrdinalIgnoreCase) || PlainText.OfChat(text).Contains(words, StringComparison.OrdinalIgnoreCase);
}
