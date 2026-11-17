using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using GSBC.ChurchApp.Platform.Ui.Locations;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class MinistryLocations(IMinistryReads reads) : ILocationSource
{
    public IReadOnlyCollection<string> Feeds { get; } = [MinistriesFeeds.Directory, MinistriesFeeds.PrivateDirectory, MinistriesFeeds.Places];

    public async Task<IReadOnlyList<Location>> LocationsAsync() =>
        [.. (await reads.DirectoryAsync()).Ministries.Select(ministry => ministry.Where).OfType<Location>()];
}
