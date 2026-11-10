namespace GSBC.ChurchApp.Platform.Contracts;

public static class CodeGroups
{
    private const int GroupLength = 4;
    private const char Separator = '-';

    public static string Format(string typed, int length) =>
        string.Join(Separator, typed
            .Where(char.IsAsciiLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .Take(length)
            .Chunk(GroupLength)
            .Select(group => new string(group)));

    public static int FormattedLength(int length) => length + (length - 1) / GroupLength;
}
