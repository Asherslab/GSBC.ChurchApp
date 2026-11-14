using System.Text.Json;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Preferences;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed class OutreachClaims(IOutbox outbox, ILocalPreferences preferences, WalkChoices choices, TimeProvider time)
{
    private const string Service = "/gsbc.outreach.Outreach/";
    private const string MineKey = "outreach.claims";

    public async Task<IReadOnlyList<Claim>> MineAsync() =>
        JsonSerializer.Deserialize(await preferences.GetAsync(MineKey) ?? "[]", OutreachPhoneJson.Default.ListPhoneClaim)?
            .Select(claim => new Claim { Id = claim.Id, RouteId = claim.RouteId, DayId = claim.DayId, FirstName = claim.FirstName, At = claim.At })
            .ToList() ?? [];

    public async Task<IReadOnlyList<Claim>> WithMineAsync(OutreachNow now)
    {
        HashSet<string> synced = [.. now.Claims.Select(claim => claim.Id)];
        HashSet<string> days = [.. now.Days.Select(day => day.Id)];
        return [.. now.Claims, .. (await MineAsync()).Where(claim => !synced.Contains(claim.Id) && days.Contains(claim.DayId))];
    }

    public async Task ClaimAsync(DayRoute route, string firstName)
    {
        string name = OutreachRules.Clip(firstName, OutreachRules.LongestFirstName);
        await choices.RememberFirstNameAsync(name);
        PhoneClaim claim = new() { Id = Guid.NewGuid().ToString(), RouteId = route.Id, DayId = route.DayId, FirstName = name, At = time.GetUtcNow().UtcDateTime };
        await outbox.EnqueueAsync(Service + "Claim", new ClaimSend { Id = claim.Id, RouteId = route.Id, FirstName = name }, OutreachContractsModel.Instance);
        await KeepAsync(mine => mine.Add(claim));
    }

    public async Task UnclaimAsync(string claimId)
    {
        await outbox.EnqueueAsync(Service + "Unclaim", new MarkRef { Id = claimId }, OutreachContractsModel.Instance);
        await KeepAsync(mine => mine.RemoveAll(claim => claim.Id == claimId));
    }

    private async Task KeepAsync(Action<List<PhoneClaim>> change)
    {
        List<PhoneClaim> mine = JsonSerializer.Deserialize(await preferences.GetAsync(MineKey) ?? "[]", OutreachPhoneJson.Default.ListPhoneClaim) ?? [];
        change(mine);
        mine.RemoveAll(claim => claim.At < time.GetUtcNow().UtcDateTime.AddDays(-2));
        await preferences.SetAsync(MineKey, JsonSerializer.Serialize(mine, OutreachPhoneJson.Default.ListPhoneClaim));
    }
}
