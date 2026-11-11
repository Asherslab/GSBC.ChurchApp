using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class MinistryPeopleWrites(IMinistryWrites writes) : IMinistryPeopleWrites
{
    public Task AddAsync(string ministryId, string personId, string place) =>
        writes.ChangePersonAsync(ministryId, personId, place == Places.Team ? Roles.Team : Roles.Attendee, true);
}
