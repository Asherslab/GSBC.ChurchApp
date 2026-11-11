using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public interface ILinkableCalendars
{
    Task<IReadOnlyList<Crumb>> ChoicesAsync(string ministryId);
}
