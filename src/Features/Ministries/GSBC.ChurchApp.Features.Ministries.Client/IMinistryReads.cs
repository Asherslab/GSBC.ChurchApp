using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public interface IMinistryReads
{
    IReadOnlyList<string> Feeds { get; }

    Task<MinistryDirectory> DirectoryAsync();

    Task<MinistryPage> MinistryAsync(string id);

    Task<LifeGroupsPage> LifeGroupsAsync();

    Task<PeopleList> PeopleAsync(string ministryId);
}

public sealed class MinistryNotHereException(string message) : Exception(message);
