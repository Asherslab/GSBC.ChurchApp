namespace GSBC.ChurchApp.Platform.Contracts.Content;

public enum LinkKind
{
    Unsafe,
    Web,
    Email,
    Phone,
    InApp,
}

public static class LinkTargets
{
    public const string MediaScheme = "media:";

    public static LinkKind KindOf(string href)
    {
        if (href.Length == 0 || href.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
            return LinkKind.Unsafe;
        if (href.StartsWith('/'))
            return href.Length > 1 && href[1] is '/' or '\\' || href.Contains('\\') ? LinkKind.Unsafe : LinkKind.InApp;
        if (!Uri.TryCreate(href, UriKind.Absolute, out Uri? uri))
            return LinkKind.Unsafe;

        return uri.Scheme switch
        {
            "https" when uri.Host.Length > 0 && href.StartsWith("https://", StringComparison.OrdinalIgnoreCase) => LinkKind.Web,
            "mailto" when href[7..].Contains('@') => LinkKind.Email,
            "tel" when href[4..].All(character => char.IsAsciiDigit(character) || character is '+' or '-' or '(' or ')') => LinkKind.Phone,
            _ => LinkKind.Unsafe,
        };
    }

    public static bool IsSafe(string href) => KindOf(href) != LinkKind.Unsafe;

    public static string? InAppRoute(string href) => KindOf(href) == LinkKind.InApp ? href.TrimStart('/') : null;
}
