using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Ui;

namespace GSBC.ChurchApp.Features.Prayer.Client;

public static class PrayerWords
{
    public const string Note = "Our pastoral team would love to pray with you.";
    public const string NoName = "No name";
    public const string Private = "Only the pastoral team reads your request, unless you also ask for it on the prayer wall.";
    public const string CantSendHere = "Prayer requests are sent from the church app on a phone.";
    public const string FromTheChurch = "From the church. Any prayer wall staff can change the request or take it down.";
    public const string ShortNameHint = "Your first name and the first letter of your last name, such as Sam S.";

    public static string Excerpt(string text, int longest = 90)
    {
        string flat = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (flat.Length <= longest)
            return flat;
        int cut = flat.LastIndexOf(' ', longest - 1);
        return flat[..(cut > 20 ? cut : longest)].TrimEnd(',', '.', ';') + "…";
    }

    public static string Day(DateTime utc) => ChurchTime.ToChurch(utc).ToString("d MMMM");

    public static string Prayed(int count) => count switch
    {
        0 => "Nobody has prayed yet",
        1 => "1 prayer",
        _ => $"{count} prayers",
    };

    public static string Status(YourLine line) => line.Sending switch
    {
        Sending.Waiting => "Waiting to send. It goes by itself when you're back online.",
        Sending.CouldNotSend => "Couldn't send",
        _ => string.Join(" · ", new[] { Where(line), Settled(line) }.OfType<string>()),
    };

    private static string? Settled(YourLine line) =>
        line.Answered ? "Answered" : line is { Closed: true, MessageOnTheWall: false } ? "Closed" : null;

    public static string Where(YourLine line) => line.Wall switch
    {
        WallState.Shown when line is { MessageOnTheWall: true, WallUntil: { } until } => $"Your message is on the prayer wall until {Day(until)} · {Prayed(line.Prayed)}",
        WallState.Shown when line.WallUntil is { } until => $"On the prayer wall until {Day(until)} · {Prayed(line.Prayed)}",
        WallState.Shown => $"On the prayer wall · {Prayed(line.Prayed)}",
        WallState.Waiting when line.Known => "With the pastoral team, who'll read your request before it goes on the wall",
        WallState.Ended => $"Was on the prayer wall · {Prayed(line.Prayed)}",
        WallState.TakenDown => "With the pastoral team. You took your request off the wall",
        _ => "With the pastoral team",
    };

    public static string FromTheDesk(string name, bool fromChurch, bool forSomeoneElse) =>
        fromChurch ? $"{name}, from the church" : forSomeoneElse ? $"{name}, written in for them" : name;

    public static string WhoAndWhen(WallPost post, DateTime now) =>
        $"{(post.Name.Length > 0 ? post.Name : NoName)} · {ChurchDates.Ago(post.ShownAt, now)}";

    public static string Waiting(int count) => count switch
    {
        0 => "Nothing waiting",
        1 => "1 waiting",
        _ => $"{count} waiting",
    };

    public static string Staff(StaffCase item, DateTime now)
    {
        if (item.Pending is { } pending)
            return pending.Failed ? $"Couldn't send: {pending.What.ToLowerInvariant()}" : $"{pending.What}. Waiting to send";
        string from = item.Care?.From ?? (item.Wall is { ShortName.Length: > 0 } wall ? wall.ShortName : NoName);
        return $"{from} · {ChurchDates.Ago(item.SentAt, now)}";
    }

    public static string Care(CareState care) => care switch
    {
        CareState.Answered => "Answered",
        CareState.Closed => "Closed by them",
        _ => "New",
    };

    public static string MessageFrom(WallLine line) =>
        line.Yours ? "Your message:" : line.Post.Name.Length > 0 ? $"Message from {line.Post.Name}:" : "Message from the person who asked:";

    public static string WallLine(WallState wall) => wall switch
    {
        WallState.Waiting => "Asks for the prayer wall",
        WallState.Shown => "On the prayer wall",
        WallState.KeptPrivate => "Kept off the prayer wall",
        WallState.Ended => "Was on the prayer wall",
        WallState.TakenDown => "They took the request off the prayer wall",
        _ => "Only for the pastoral team",
    };
}
