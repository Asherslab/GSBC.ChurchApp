using GSBC.ChurchApp.Platform.Client.People;

namespace GSBC.ChurchApp.Features.Access.Client;

internal sealed class AccessRoleDirectory(AccessClient access) : IRoleDirectory
{
    public async Task<IReadOnlyList<RoleSummary>> ListAsync() =>
        (await access.ListRoleNamesAsync()).Roles
            .Where(role => Guid.TryParse(role.Id, out _))
            .Select(role => new RoleSummary(Guid.Parse(role.Id), role.Name, role.Description))
            .ToList();
}
