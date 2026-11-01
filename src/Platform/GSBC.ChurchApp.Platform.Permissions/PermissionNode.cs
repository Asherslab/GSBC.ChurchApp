namespace GSBC.ChurchApp.Platform.Permissions;

public static class PermissionNode
{
    public const char Separator = '.';

    public static bool IsValid(string node) =>
        node.Length > 0 && node.Split(Separator).All(IsValidSegment);

    internal static bool IsValidSegment(string segment) =>
        segment.Length > 0 && segment.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');
}
