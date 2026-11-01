using System.Diagnostics.CodeAnalysis;

namespace GSBC.ChurchApp.Platform.Permissions;

public sealed record PermissionPattern
{
    private const char DenyPrefix = '-';
    private const string Wildcard = "*";

    private readonly string[] _segments;

    private PermissionPattern(string text, bool isDeny, string[] segments, bool isWildcard)
    {
        Text = text;
        IsDeny = isDeny;
        _segments = segments;
        IsWildcard = isWildcard;
    }

    public string Text { get; }
    public bool IsDeny { get; }
    public bool IsWildcard { get; }

    public int Specificity => _segments.Length * 2 + (IsWildcard ? 0 : 1);

    public static PermissionPattern Parse(string text) =>
        TryParse(text, out PermissionPattern? pattern)
            ? pattern
            : throw new FormatException($"'{text}' is not a permission pattern.");

    public static bool TryParse(string text, [NotNullWhen(true)] out PermissionPattern? pattern)
    {
        pattern = null;
        bool isDeny = text.StartsWith(DenyPrefix);
        string body = isDeny ? text[1..] : text;
        string[] segments = body.Split(PermissionNode.Separator);
        bool isWildcard = segments[^1] == Wildcard;
        string[] fixedSegments = isWildcard ? segments[..^1] : segments;

        if (!isWildcard && fixedSegments.Length == 0)
            return false;
        if (!fixedSegments.All(PermissionNode.IsValidSegment))
            return false;

        pattern = new PermissionPattern(text, isDeny, fixedSegments, isWildcard);
        return true;
    }

    public bool Matches(string node)
    {
        string[] nodeSegments = node.Split(PermissionNode.Separator);

        if (IsWildcard ? nodeSegments.Length <= _segments.Length : nodeSegments.Length != _segments.Length)
            return false;

        return _segments.AsSpan().SequenceEqual(nodeSegments.AsSpan(0, _segments.Length));
    }

    public override string ToString() => Text;
}
