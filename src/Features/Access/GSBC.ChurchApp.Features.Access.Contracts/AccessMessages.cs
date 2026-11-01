using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Access.Contracts;

[ProtoContract]
public sealed class Nothing;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class GrantList
{
    [ProtoMember(1)] public List<GrantMessage> Grants { get; set; } = [];
}

[ProtoContract]
public sealed class GrantMessage
{
    [ProtoMember(1)] public string Pattern { get; set; } = "";
    [ProtoMember(2)] public string ScopeKind { get; set; } = "";
    [ProtoMember(3)] public string? ObjectId { get; set; }
}

[ProtoContract]
public sealed class Catalog
{
    [ProtoMember(1)] public List<PermissionInfo> Permissions { get; set; } = [];
    [ProtoMember(2)] public List<string> ScopeKinds { get; set; } = [];
}

[ProtoContract]
public sealed class PermissionInfo
{
    [ProtoMember(1)] public string Node { get; set; } = "";
    [ProtoMember(2)] public string ScopeKind { get; set; } = "";
    [ProtoMember(3)] public string Description { get; set; } = "";
    [ProtoMember(4)] public bool Sensitive { get; set; }
}

[ProtoContract]
public sealed class RoleList
{
    [ProtoMember(1)] public List<RoleInfo> Roles { get; set; } = [];
}

[ProtoContract]
public sealed class RoleInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Description { get; set; } = "";
    [ProtoMember(4)] public string ScopeKind { get; set; } = "";
    [ProtoMember(5)] public bool IsSystem { get; set; }
    [ProtoMember(6)] public List<string> Patterns { get; set; } = [];
    [ProtoMember(7)] public List<string> IncludedRoleIds { get; set; } = [];
}

[ProtoContract]
public sealed class RoleReference
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class AssignmentQuery
{
    [ProtoMember(1)] public string? UserId { get; set; }
}

[ProtoContract]
public sealed class AssignmentList
{
    [ProtoMember(1)] public List<AssignmentInfo> Assignments { get; set; } = [];
}

[ProtoContract]
public sealed class AssignmentInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string UserId { get; set; } = "";
    [ProtoMember(3)] public string RoleId { get; set; } = "";
    [ProtoMember(4)] public string RoleName { get; set; } = "";
    [ProtoMember(5)] public string? ScopeObjectId { get; set; }
    [ProtoMember(6)] public DateTime? ExpiresAt { get; set; }
    [ProtoMember(7)] public string Source { get; set; } = "";
    [ProtoMember(8)] public DateTime CreatedAt { get; set; }
}

[ProtoContract]
public sealed class AssignRequest : IScopedRequest
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
    [ProtoMember(2)] public string RoleId { get; set; } = "";
    [ProtoMember(3)] public string? ScopeObjectId { get; set; }
    [ProtoMember(4)] public DateTime? ExpiresAt { get; set; }

    public string ScopeId => ScopeObjectId ?? "";
}

[ProtoContract]
public sealed class AssignmentReference
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}
