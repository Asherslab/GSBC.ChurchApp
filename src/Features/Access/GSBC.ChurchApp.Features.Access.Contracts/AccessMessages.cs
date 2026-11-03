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
    [ProtoMember(5)] public string Area { get; set; } = "";
}

[ProtoContract]
public sealed class RoleName
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Description { get; set; } = "";
}

[ProtoContract]
public sealed class RoleNameList
{
    [ProtoMember(1)] public List<RoleName> Roles { get; set; } = [];
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
    [ProtoMember(8)] public List<string> AbilityNodes { get; set; } = [];
    [ProtoMember(9)] public bool AppliesToChildren { get; set; }
    [ProtoMember(10)] public List<string> AssignableByRoleIds { get; set; } = [];
    [ProtoMember(11)] public int HolderCount { get; set; }
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

[ProtoContract]
public sealed class PersonReference
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
}

[ProtoContract]
public sealed class PersonAbilities
{
    [ProtoMember(1)] public List<PlaceAbilities> Places { get; set; } = [];
}

[ProtoContract]
public sealed class PlaceAbilities
{
    [ProtoMember(1)] public string ScopeKind { get; set; } = "";
    [ProtoMember(2)] public string? ObjectId { get; set; }
    [ProtoMember(3)] public bool IncludesTeams { get; set; }
    [ProtoMember(4)] public List<string> Nodes { get; set; } = [];
    [ProtoMember(5)] public List<PlaceReason> Because { get; set; } = [];
}

[ProtoContract]
public sealed class PlaceReason
{
    [ProtoMember(1)] public string RoleName { get; set; } = "";
    [ProtoMember(2)] public DateTime? ExpiresAt { get; set; }
}

[ProtoContract]
public sealed class RolePreviewRequest
{
    [ProtoMember(1, IsRequired = true)] public RoleInfo Role { get; set; } = new();
    [ProtoMember(2)] public bool Deleting { get; set; }
}

[ProtoContract]
public sealed class RoleEffect
{
    [ProtoMember(1)] public List<PersonEffect> People { get; set; } = [];
}

[ProtoContract]
public sealed class PersonEffect
{
    [ProtoMember(1)] public string UserId { get; set; } = "";
    [ProtoMember(2)] public List<AbilityChange> Gains { get; set; } = [];
    [ProtoMember(3)] public List<AbilityChange> Losses { get; set; } = [];
}

[ProtoContract]
public sealed class AbilityChange
{
    [ProtoMember(1)] public string Node { get; set; } = "";
    [ProtoMember(2)] public string? ObjectId { get; set; }
}
