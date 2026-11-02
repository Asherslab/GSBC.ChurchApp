namespace GSBC.ChurchApp.Features.Access.Api.Data;

public sealed class RoleRecord
{
    public Guid Id { get; set; }
    public string? SystemKey { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public required string ScopeKind { get; set; }
    public bool AppliesToChildren { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RolePattern> Patterns { get; set; } = [];
    public List<RoleInclude> Includes { get; set; } = [];
    public List<RoleAssigner> Assigners { get; set; } = [];

    public bool IsSystem => SystemKey is not null;
}

public sealed class RolePattern
{
    public Guid RoleId { get; set; }
    public required string Pattern { get; set; }
}

public sealed class RoleInclude
{
    public Guid RoleId { get; set; }
    public Guid IncludedRoleId { get; set; }
}

public sealed class RoleAssigner
{
    public Guid RoleId { get; set; }
    public Guid AssignerRoleId { get; set; }
}

public sealed class Assignment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public RoleRecord? Role { get; set; }
    public string? ScopeObjectId { get; set; }
    public AssignmentSource Source { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}

public enum AssignmentSource
{
    Manual,
    Bootstrap,
    Derived,
}

public sealed class CatalogEntry
{
    public required string Node { get; set; }
    public required string ScopeKind { get; set; }
    public string Description { get; set; } = "";
    public bool Sensitive { get; set; }
}
