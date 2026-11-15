using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal static class CampaignTelling
{
    private const string Tracts = "tracts";

    public static bool ShouldTell(CampaignRecord campaign) => !campaign.General && campaign.Stage == CampaignStage.WrappingUp && campaign.ToldEndingAt is null;

    public static string Title(CampaignRecord campaign) =>
        campaign.EndsOn is { } ends ? $"{campaign.Name} ends on {ends.ToString("ddd d MMM", CultureInfo.InvariantCulture)}" : $"{campaign.Name} is ending soon";

    public static string Body(CampaignRecord campaign) => $"Help get the last {Handed(campaign.HandedOut)} out before it ends.";

    private static string Handed(string handedOut) =>
        handedOut.Length == 0 ? Tracts
        : handedOut.Length > 1 && char.IsUpper(handedOut[0]) && !char.IsUpper(handedOut[1]) ? char.ToLowerInvariant(handedOut[0]) + handedOut[1..]
        : handedOut;
}
