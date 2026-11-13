namespace GSBC.ChurchApp.Platform.Contracts.Content;

public static class Emoji
{
    public static readonly IReadOnlyList<(string Code, string Character)> Common =
    [
        ("smile", "😊"), ("grinning", "😀"), ("joy", "😂"), ("wink", "😉"), ("heart_eyes", "😍"), ("hugs", "🤗"), ("thinking", "🤔"), ("sunglasses", "😎"),
        ("cry", "😢"), ("pray", "🙏"), ("raised_hands", "🙌"), ("clap", "👏"), ("wave", "👋"), ("thumbsup", "👍"), ("ok_hand", "👌"), ("muscle", "💪"),
        ("handshake", "🤝"), ("point_right", "👉"), ("point_down", "👇"), ("eyes", "👀"), ("heart", "❤️"), ("blue_heart", "💙"), ("green_heart", "💚"), ("sparkles", "✨"),
        ("star", "⭐"), ("star2", "🌟"), ("fire", "🔥"), ("tada", "🎉"), ("balloon", "🎈"), ("gift", "🎁"), ("birthday", "🎂"), ("christmas_tree", "🎄"),
        ("candle", "🕯️"), ("latin_cross", "✝️"), ("dove", "🕊️"), ("church", "⛪"), ("angel", "👼"), ("open_book", "📖"), ("books", "📚"), ("notes", "🎶"),
        ("microphone", "🎤"), ("guitar", "🎸"), ("calendar", "📅"), ("clock", "🕒"), ("bell", "🔔"), ("loudspeaker", "📢"), ("pushpin", "📌"), ("round_pushpin", "📍"),
        ("memo", "📝"), ("pencil", "✏️"), ("email", "📧"), ("phone", "📞"), ("house", "🏠"), ("car", "🚗"), ("bus", "🚌"), ("tent", "⛺"),
        ("sunny", "☀️"), ("rainbow", "🌈"), ("umbrella", "☔"), ("snowflake", "❄️"), ("earth", "🌏"), ("seedling", "🌱"), ("sunflower", "🌻"), ("rose", "🌹"),
        ("coffee", "☕"), ("tea", "🍵"), ("cake", "🍰"), ("pizza", "🍕"), ("bread", "🍞"), ("apple", "🍎"), ("baby", "👶"), ("family", "👪"),
        ("soccer", "⚽"), ("basketball", "🏀"), ("white_check_mark", "✅"), ("x", "❌"), ("warning", "⚠️"), ("hundred", "💯"), ("arrow_right", "➡️"), ("question", "❓"),
    ];

    private static readonly Dictionary<string, string> ByCode = Common.ToDictionary(entry => entry.Code, entry => entry.Character, StringComparer.Ordinal);

    public static string? Of(string code) => ByCode.GetValueOrDefault(code);
}
