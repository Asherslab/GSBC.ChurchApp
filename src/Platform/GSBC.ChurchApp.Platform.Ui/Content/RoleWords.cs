namespace GSBC.ChurchApp.Platform.Ui.Content;

public static class RoleWords
{
    public static string List(IReadOnlyList<Guid> roleIds, IReadOnlyDictionary<Guid, string> names)
    {
        List<string> named = roleIds.Select(role => names.GetValueOrDefault(role, "people in a role")).ToList();
        return named.Count switch
        {
            0 => "people in a role",
            1 => named[0],
            2 => $"{named[0]} and {named[1]}",
            3 => $"{named[0]}, {named[1]} and {named[2]}",
            _ => $"{named[0]}, {named[1]} and {named.Count - 2} more roles",
        };
    }
}
