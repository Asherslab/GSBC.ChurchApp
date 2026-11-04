using GSBC.ChurchApp.Features.Media.Contracts;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class FolderMap
{
    private readonly Dictionary<string, FolderInfo> _byId;
    private readonly ILookup<string, FolderInfo> _children;

    public FolderMap(FolderTree tree)
    {
        _byId = tree.Folders.ToDictionary(folder => folder.Id);
        _children = tree.Folders.ToLookup(folder => folder.ParentId is { } parent && _byId.ContainsKey(parent) ? parent : "");
    }

    public static FolderMap Empty { get; } = new(new FolderTree());

    public bool HasSystemFolders => _children[""].Any(folder => folder.IsSystem);

    public FolderInfo? Find(string? id) => id is null ? null : _byId.GetValueOrDefault(id);

    public IEnumerable<FolderInfo> ChildrenOf(string? id, bool system) =>
        _children[id ?? ""].Where(folder => folder.IsSystem == system);

    public IReadOnlyList<FolderInfo> PathTo(string? id)
    {
        List<FolderInfo> path = [];
        for (FolderInfo? folder = Find(id); folder is not null && path.Count <= _byId.Count; folder = Find(folder.ParentId))
            path.Insert(0, folder);
        return path;
    }

    public string PathName(string? id) => PathTo(id) is { Count: > 0 } path ? string.Join(" › ", path.Select(folder => folder.Name)) : "Library";

    public bool IsWithin(string? id, string ancestorId) => PathTo(id).Any(folder => folder.Id == ancestorId);

    public IEnumerable<(FolderInfo Folder, int Depth)> StaffFolders(string? under = null, int depth = 0)
    {
        foreach (FolderInfo folder in ChildrenOf(under, system: false))
        {
            yield return (folder, depth);
            foreach ((FolderInfo, int) child in StaffFolders(folder.Id, depth + 1))
                yield return child;
        }
    }
}
