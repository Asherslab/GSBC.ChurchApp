namespace GSBC.ChurchApp.Platform.Client.People;

public sealed record RoleSummary(Guid Id, string Name, string Description);

public interface IRoleDirectory
{
    Task<IReadOnlyList<RoleSummary>> ListAsync();
}
