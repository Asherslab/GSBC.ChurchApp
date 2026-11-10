using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public interface ILatestSermonsOnline
{
    Task<IReadOnlyList<Sermon>> LatestAsync(int count);
}
