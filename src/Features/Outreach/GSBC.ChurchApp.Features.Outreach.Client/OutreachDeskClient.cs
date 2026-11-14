using Grpc.Core;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Outreach.Client;

[AdminOnly]
public sealed class OutreachDeskClient(ChannelBase channel)
{
    private readonly IOutreachAdminService _admin = channel.CreateGrpcService<IOutreachAdminService>();

    public async Task<OutreachDesk> DeskAsync(string ministryId) => await _admin.DeskAsync(new MinistryRef { MinistryId = ministryId });

    public async Task SaveSettingsAsync(string ministryId, OutreachSettings settings) =>
        await _admin.SaveSettingsAsync(new SettingsSave { MinistryId = ministryId, Settings = settings });

    public async Task SaveCampaignAsync(string ministryId, Campaign campaign) =>
        await _admin.SaveCampaignAsync(new CampaignSave { MinistryId = ministryId, Campaign = campaign });

    public async Task<CampaignDeletion> CampaignDeletionAsync(string ministryId, string id) =>
        await _admin.CampaignDeletionAsync(new ThingRef { MinistryId = ministryId, Id = id });

    public async Task DeleteCampaignAsync(string ministryId, string id) => await _admin.DeleteCampaignAsync(new ThingRef { MinistryId = ministryId, Id = id });
}
