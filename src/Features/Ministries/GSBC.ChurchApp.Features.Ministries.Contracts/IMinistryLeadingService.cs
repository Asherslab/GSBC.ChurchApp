using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

[Service("gsbc.ministries.Leading")]
public interface IMinistryLeadingService
{
    [Requires(MinistriesPermissions.PeopleNode)]
    ValueTask<PeopleList> PeopleAsync(MinistryRef request, CallContext context = default);

    [Requires(MinistriesPermissions.PeopleNode)]
    ValueTask<Done> AnswerAsync(AskRef request, CallContext context = default);

    [Requires(MinistriesPermissions.PeopleNode)]
    ValueTask<Done> ChangePersonAsync(PersonChange request, CallContext context = default);

    [Requires(MinistriesPermissions.PeopleNode)]
    ValueTask<FoundPeople> FindAsync(FindRequest request, CallContext context = default);

    [Requires(MinistriesPermissions.PeopleNode)]
    ValueTask<Done> LinkParentAsync(LinkParentRequest request, CallContext context = default);

    [Requires(MinistriesPermissions.PostNode)]
    ValueTask<Post> SavePostAsync(PostSave request, CallContext context = default);

    [Requires(MinistriesPermissions.PostNode)]
    ValueTask<Done> DeletePostAsync(ItemRef request, CallContext context = default);

    [Requires(MinistriesPermissions.PostNode)]
    ValueTask<FileItem> SaveFileAsync(FileSave request, CallContext context = default);

    [Requires(MinistriesPermissions.PostNode)]
    ValueTask<Done> DeleteFileAsync(ItemRef request, CallContext context = default);

    [Requires(MinistriesPermissions.SettingsNode)]
    ValueTask<MinistryDetails> SaveMinistryAsync(MinistrySave request, CallContext context = default);

    [Requires(MinistriesPermissions.SettingsNode)]
    ValueTask<MinistryDetails> SavePartsAsync(PartsSave request, CallContext context = default);

    [Requires(MinistriesPermissions.SettingsNode)]
    ValueTask<MinistryDetails> AddSubMinistryAsync(MinistrySave request, CallContext context = default);

    [Requires(MinistriesPermissions.CreateNode)]
    ValueTask<MinistryDetails> CreateAsync(MinistrySave request, CallContext context = default);

    [SignedIn]
    ValueTask<DeletionPreview> DeletionAsync(MinistryRef request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> DeleteAsync(MinistryRef request, CallContext context = default);

    [Requires(MinistriesPermissions.SettingsNode)]
    ValueTask<MovePreview> MovingAsync(MoveRequest request, CallContext context = default);

    [Requires(MinistriesPermissions.SettingsNode)]
    ValueTask<MinistryDetails> MoveAsync(MoveRequest request, CallContext context = default);

    [Requires(MinistriesPermissions.SettingsNode)]
    ValueTask<CalendarLine> SaveCalendarAsync(CalendarSave request, CallContext context = default);

    [Requires(MinistriesPermissions.SettingsNode)]
    ValueTask<CalendarRemoval> CalendarRemovalAsync(ItemRef request, CallContext context = default);

    [Requires(MinistriesPermissions.SettingsNode)]
    ValueTask<Done> RemoveCalendarAsync(ItemRef request, CallContext context = default);

    [Requires(MinistriesPermissions.PlanLifeGroupsNode)]
    ValueTask<Round> SaveRoundAsync(RoundSave request, CallContext context = default);

    [Requires(MinistriesPermissions.PlanLifeGroupsNode)]
    ValueTask<Done> DeleteRoundAsync(RoundRef request, CallContext context = default);

    [Requires(MinistriesPermissions.PostNode)]
    ValueTask<Done> SaveSessionAsync(SessionSave request, CallContext context = default);
}
