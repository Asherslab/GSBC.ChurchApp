using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public static class OutreachScopes
{
    public static async Task<string?> FindAsync(OutreachDeskClient outreach, IAccess access)
    {
        RunningMinistries running = await outreach.MinistriesAsync();
        return access.Can(OutreachPermissions.Reports)
            ? running.Ministries.FirstOrDefault()?.Id ?? ""
            : running.Ministries.FirstOrDefault(ministry => access.Can(OutreachPermissions.Reports, ministry.Id))?.Id;
    }
}
