using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Uploads;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Ministries.Admin;

public sealed class OnlineMinistries(ChannelBase channel) : IMinistryReads, IMinistryWrites, ILinkableCalendars
{
    private readonly IMinistriesService _ministries = channel.CreateGrpcService<IMinistriesService>();
    private readonly IMinistryLeadingService _leading = channel.CreateGrpcService<IMinistryLeadingService>();

    public IReadOnlyList<string> Feeds { get; } = [MinistriesFeeds.Ministries];

    public async Task<MinistryDirectory> DirectoryAsync() => await _ministries.DirectoryAsync(new DirectoryRequest());

    public async Task<MinistryPage> MinistryAsync(string id) => await _ministries.MinistryAsync(new MinistryRef { MinistryId = id });

    public async Task<LifeGroupsPage> LifeGroupsAsync() => await _ministries.LifeGroupsAsync(new LifeGroupsRequest());

    public async Task<PeopleList> PeopleAsync(string ministryId) => await _leading.PeopleAsync(new MinistryRef { MinistryId = ministryId });

    public async Task ParentAnswerAsync(string ministryId, string askId, bool yes) =>
        await _ministries.ParentAnswerAsync(new AskRef { MinistryId = ministryId, AskId = askId, Yes = yes });

    public async Task<JoinOutcome> JoinAsync(MinistryDetails ministry, string personId, string place, string note, bool parentFirst = false) =>
        await _ministries.JoinAsync(new JoinRequest { MinistryId = ministry.Id, PersonId = personId, As = place, Note = note });

    public async Task LeaveAsync(string ministryId, string personId, string place) =>
        await _ministries.LeaveAsync(new JoinRequest { MinistryId = ministryId, PersonId = personId, As = place });

    public async Task WithdrawAsync(string ministryId, string askId) => await _ministries.WithdrawAsync(new AskRef { MinistryId = ministryId, AskId = askId });

    public async Task AnswerAsync(string ministryId, string askId, bool yes) =>
        await _leading.AnswerAsync(new AskRef { MinistryId = ministryId, AskId = askId, Yes = yes });

    public async Task SetRoleAsync(string ministryId, string personId, string role) =>
        await _leading.SetRoleAsync(new PersonChange { MinistryId = ministryId, PersonId = personId, Role = role });

    public async Task ChangePersonAsync(string ministryId, string personId, string role, bool add) =>
        await _leading.ChangePersonAsync(new PersonChange { MinistryId = ministryId, PersonId = personId, Role = role, Add = add });

    public async Task LinkParentAsync(string ministryId, string childId, string parentId) =>
        await _leading.LinkParentAsync(new LinkParentRequest { MinistryId = ministryId, ChildId = childId, ParentId = parentId });

    public async Task SavePostAsync(string ministryId, Post post) => await _leading.SavePostAsync(new PostSave { MinistryId = ministryId, Post = post });

    public async Task DeletePostAsync(string ministryId, string id) => await _leading.DeletePostAsync(new ItemRef { MinistryId = ministryId, Id = id });

    public async Task SaveFileAsync(string ministryId, FileItem file, QueuedUpload? upload = null) =>
        await _leading.SaveFileAsync(new FileSave { MinistryId = ministryId, File = upload is null ? file : throw new InvalidOperationException("Adding a file from this computer isn't built yet. Add a link instead.") });

    public async Task DeleteFileAsync(string ministryId, string id) => await _leading.DeleteFileAsync(new ItemRef { MinistryId = ministryId, Id = id });

    public async Task<MinistryDetails> SaveMinistryAsync(MinistryDetails details) =>
        await _leading.SaveMinistryAsync(new MinistrySave { MinistryId = details.Id, Details = details });

    public async Task SavePartsAsync(string ministryId, IEnumerable<string> partsOff, IEnumerable<string> partsFromBelow, string chatStarters = "",
        IEnumerable<string>? partsOrder = null) =>
        await _leading.SavePartsAsync(new PartsSave
        {
            MinistryId = ministryId, PartsOff = [.. partsOff], PartsFromBelow = [.. partsFromBelow], ChatStarters = chatStarters, PartsOrder = [.. partsOrder ?? []],
        });

    public async Task MoveAsync(string ministryId, string underId) => await _leading.MoveAsync(new MoveRequest { MinistryId = ministryId, UnderId = underId });

    public async Task<MinistryDetails> AddSubMinistryAsync(string parentId, MinistryDetails details) =>
        await _leading.AddSubMinistryAsync(new MinistrySave { MinistryId = parentId, Details = details });

    public async Task<MinistryDetails> CreateAsync(MinistryDetails details) => await _leading.CreateAsync(new MinistrySave { Details = details });

    public async Task DeleteAsync(string ministryId) => await _leading.DeleteAsync(new MinistryRef { MinistryId = ministryId });

    public async Task ArchiveAsync(string ministryId) => await _leading.ArchiveAsync(new MinistryRef { MinistryId = ministryId });

    public async Task RestoreAsync(string ministryId) => await _leading.RestoreAsync(new MinistryRef { MinistryId = ministryId });

    public async Task<ArchivedList> ArchivedAsync() => await _leading.ArchivedAsync(new DirectoryRequest());

    public async Task SaveCalendarAsync(string ministryId, CalendarLine calendar) =>
        await _leading.SaveCalendarAsync(new CalendarSave { MinistryId = ministryId, Calendar = calendar });

    public async Task LinkCalendarAsync(string ministryId, string calendarId, bool linked) =>
        await _leading.LinkCalendarAsync(new CalendarLinkSave { MinistryId = ministryId, CalendarId = calendarId, Linked = linked });

    public async Task<IReadOnlyList<Crumb>> ChoicesAsync(string ministryId) =>
        (await _leading.LinkableCalendarsAsync(new MinistryRef { MinistryId = ministryId })).Calendars;

    public async Task RemoveCalendarAsync(string ministryId, string id) => await _leading.RemoveCalendarAsync(new ItemRef { MinistryId = ministryId, Id = id });



    public async Task SitOutAsync(SittingOut sittingOut, bool off) => await _leading.SitOutAsync(new SitOutSave { SittingOut = sittingOut, Off = off });

    public async Task<LifeGroupsCalendar> LifeGroupsCalendarAsync() => await _leading.LifeGroupsCalendarAsync(new LifeGroupsRequest());

    public async Task<LifeGroupsCalendar> ShowLifeGroupsInAsync(string calendarId) =>
        await _leading.ShowLifeGroupsInAsync(new LifeGroupsCalendarChoice { CalendarId = calendarId });

    public async Task SaveSessionAsync(string groupId, SessionChange change) =>
        await _leading.SaveSessionAsync(new SessionSave { MinistryId = groupId, Change = change });

    public async Task SaveRoundAsync(Round round) => await _leading.SaveRoundAsync(new RoundSave { Round = round });

    public async Task DeleteRoundAsync(string id) => await _leading.DeleteRoundAsync(new RoundRef { Id = id });
}
