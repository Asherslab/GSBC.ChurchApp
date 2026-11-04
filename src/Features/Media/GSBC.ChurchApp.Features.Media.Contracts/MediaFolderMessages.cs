using ProtoBuf;

namespace GSBC.ChurchApp.Features.Media.Contracts;

[ProtoContract]
public sealed class FolderQuery;

[ProtoContract]
public sealed class FolderTree
{
    [ProtoMember(1)] public List<FolderInfo> Folders { get; set; } = [];
}

[ProtoContract]
public sealed class FolderInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string? ParentId { get; set; }
    [ProtoMember(3)] public string Name { get; set; } = "";
    [ProtoMember(4)] public bool IsSystem { get; set; }
    [ProtoMember(5)] public string Caption { get; set; } = "";
    [ProtoMember(6)] public int Files { get; set; }
    [ProtoMember(7)] public bool CanOpen { get; set; }
}

[ProtoContract]
public sealed class CreateFolderRequest
{
    [ProtoMember(1)] public string? ParentId { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class RenameFolderRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class MoveRequest
{
    [ProtoMember(1)] public List<string> ItemIds { get; set; } = [];
    [ProtoMember(2)] public List<string> FolderIds { get; set; } = [];
    [ProtoMember(3)] public string? Into { get; set; }
}

[ProtoContract]
public sealed class DeleteFolderRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public bool MoveContents { get; set; }
    [ProtoMember(3)] public string? MoveContentsInto { get; set; }
}
