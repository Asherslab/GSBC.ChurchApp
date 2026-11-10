using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class MinistriesClient(ChannelBase channel)
{
    private readonly IMinistriesService _ministries = channel.CreateGrpcService<IMinistriesService>();
    private readonly IMinistryLeadingService _leading = channel.CreateGrpcService<IMinistryLeadingService>();

    public async Task<MinistryDirectory> DirectoryAsync() => await _ministries.DirectoryAsync(new DirectoryRequest());

    public async Task<MinistryPage> MinistryAsync(string id) => await _ministries.MinistryAsync(new MinistryRef { MinistryId = id });

    public async Task<LifeGroupsPage> LifeGroupsAsync() => await _ministries.LifeGroupsAsync(new LifeGroupsRequest());

    public async Task<JoinOutcome> JoinAsync(string ministryId, string personId, string place, string note) =>
        await _ministries.JoinAsync(new JoinRequest { MinistryId = ministryId, PersonId = personId, As = place, Note = note });

    public async Task LeaveAsync(string ministryId, string personId, string place) =>
        await _ministries.LeaveAsync(new JoinRequest { MinistryId = ministryId, PersonId = personId, As = place });

    public async Task WithdrawAsync(string ministryId, string askId) => await _ministries.WithdrawAsync(new AskRef { MinistryId = ministryId, AskId = askId });

    public async Task<PeopleList> PeopleAsync(string ministryId) => await _leading.PeopleAsync(new MinistryRef { MinistryId = ministryId });

    public async Task AnswerAsync(string ministryId, string askId, bool yes) =>
        await _leading.AnswerAsync(new AskRef { MinistryId = ministryId, AskId = askId, Yes = yes });

    public async Task ChangePersonAsync(string ministryId, string personId, string role, bool add) =>
        await _leading.ChangePersonAsync(new PersonChange { MinistryId = ministryId, PersonId = personId, Role = role, Add = add });

    public async Task<IReadOnlyList<FoundPerson>> FindAsync(string ministryId, string query) =>
        (await _leading.FindAsync(new FindRequest { MinistryId = ministryId, Query = query })).People;

    public async Task LinkParentAsync(string ministryId, string childId, string parentId) =>
        await _leading.LinkParentAsync(new LinkParentRequest { MinistryId = ministryId, ChildId = childId, ParentId = parentId });

    public async Task<Post> SavePostAsync(string ministryId, Post post) => await _leading.SavePostAsync(new PostSave { MinistryId = ministryId, Post = post });

    public async Task DeletePostAsync(string ministryId, string id) => await _leading.DeletePostAsync(new ItemRef { MinistryId = ministryId, Id = id });

    public async Task<FileItem> SaveFileAsync(string ministryId, FileItem file) => await _leading.SaveFileAsync(new FileSave { MinistryId = ministryId, File = file });

    public async Task DeleteFileAsync(string ministryId, string id) => await _leading.DeleteFileAsync(new ItemRef { MinistryId = ministryId, Id = id });

    public async Task<MinistryDetails> SaveMinistryAsync(MinistryDetails details) =>
        await _leading.SaveMinistryAsync(new MinistrySave { MinistryId = details.Id, Details = details });

    public async Task<MinistryDetails> AddSubMinistryAsync(string parentId, MinistryDetails details) =>
        await _leading.AddSubMinistryAsync(new MinistrySave { MinistryId = parentId, Details = details });

    public async Task<MinistryDetails> CreateAsync(MinistryDetails details) => await _leading.CreateAsync(new MinistrySave { Details = details });

    public async Task<DeletionPreview> DeletionAsync(string ministryId) => await _leading.DeletionAsync(new MinistryRef { MinistryId = ministryId });

    public async Task DeleteAsync(string ministryId) => await _leading.DeleteAsync(new MinistryRef { MinistryId = ministryId });

    public async Task SaveCalendarAsync(string ministryId, CalendarLine calendar) =>
        await _leading.SaveCalendarAsync(new CalendarSave { MinistryId = ministryId, Calendar = calendar });

    public async Task<CalendarRemoval> CalendarRemovalAsync(string ministryId, string id) =>
        await _leading.CalendarRemovalAsync(new ItemRef { MinistryId = ministryId, Id = id });

    public async Task RemoveCalendarAsync(string ministryId, string id) => await _leading.RemoveCalendarAsync(new ItemRef { MinistryId = ministryId, Id = id });

    public async Task SaveRoundAsync(string ministryId, Round round) => await _leading.SaveRoundAsync(new RoundSave { MinistryId = ministryId, Round = round });

    public async Task DeleteRoundAsync(string ministryId, string id) => await _leading.DeleteRoundAsync(new ItemRef { MinistryId = ministryId, Id = id });

    public async Task SaveSessionAsync(string groupId, SessionChange change) =>
        await _leading.SaveSessionAsync(new SessionSave { MinistryId = groupId, Change = change });

    public static string Problem(RpcException exception) =>
        exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded
            ? "Couldn't reach the church. Check you're online and try again."
            : exception.Status.Detail.Length > 0 ? exception.Status.Detail : "That didn't work. Try again.";
}
