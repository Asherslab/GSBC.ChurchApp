using GSBC.ChurchApp.Features.People.Client;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.People.Admin;

internal sealed class SafeguardingOfficeWeek(PeopleClient people, IAccess access, TimeProvider clock) : IOfficeWeekSource
{
    public IReadOnlyCollection<string> Feeds { get; } = [PeopleFeeds.Safeguarding, PeopleFeeds.Family];

    public async Task<OfficeWeek> LoadAsync(DateTime from, DateTime until)
    {
        if (!access.Can(PeoplePermissions.SafeguardingView))
            return OfficeWeek.Nothing;

        DateTime now = clock.GetUtcNow().UtcDateTime;
        List<OfficeTask> tasks = (await people.GetFlaggedAsync())
            .Where(flagged => flagged.Kind is FlagKinds.RunOut or FlagKinds.NoParent)
            .Select(flagged => new OfficeTask(flagged.Name, flagged.Words, "safeguarding",
                flagged.Kind == FlagKinds.NoParent ? "Link a parent" : "Record the new one", flagged.When ?? now))
            .ToList();
        return new OfficeWeek(tasks, []);
    }
}

internal static class SafeguardingHref
{
    public static string For(FlaggedPerson flagged) =>
        $"people/{flagged.PersonId}?tab={(flagged.Kind == FlagKinds.NoParent ? PeopleAdminExtensions.FamilyTab : PeopleAdminExtensions.SafeguardingTab)}";
}
