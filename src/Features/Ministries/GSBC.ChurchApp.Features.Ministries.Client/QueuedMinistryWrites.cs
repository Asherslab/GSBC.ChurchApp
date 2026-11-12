using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Uploads;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class QueuedMinistryWrites(IOutbox outbox) : IMinistryWrites
{
    public async Task<JoinOutcome> JoinAsync(MinistryDetails ministry, string personId, string place, string note, bool parentFirst = false)
    {
        await SendAsync(MinistryMethods.Join, new JoinRequest { MinistryId = ministry.Id, PersonId = personId, As = place, Note = note });
        bool open = (place == Places.Team ? ministry.TeamJoin : ministry.AttendJoin) == Joining.Open;
        return parentFirst
            ? new JoinOutcome { Asked = true, Words = "Asked. A parent of yours says yes first, and you'll hear in the app." }
            : open
            ? new JoinOutcome { Joined = true, Words = $"You're in {ministry.Name}." }
            : new JoinOutcome { Asked = true, Words = "Asked. A leader says yes or no, and you'll hear in the app." };
    }

    public Task LeaveAsync(string ministryId, string personId, string place) =>
        SendAsync(MinistryMethods.Leave, new JoinRequest { MinistryId = ministryId, PersonId = personId, As = place });

    public Task ParentAnswerAsync(string ministryId, string askId, bool yes) =>
        SendAsync(MinistryMethods.ParentAnswer, new AskRef { MinistryId = ministryId, AskId = askId, Yes = yes });

    public Task WithdrawAsync(string ministryId, string askId) => SendAsync(MinistryMethods.Withdraw, new AskRef { MinistryId = ministryId, AskId = askId });

    public Task AnswerAsync(string ministryId, string askId, bool yes) => SendAsync(MinistryMethods.Answer, new AskRef { MinistryId = ministryId, AskId = askId, Yes = yes });

    public Task SetRoleAsync(string ministryId, string personId, string role) =>
        SendAsync(MinistryMethods.SetRole, new PersonChange { MinistryId = ministryId, PersonId = personId, Role = role });

    public Task ChangePersonAsync(string ministryId, string personId, string role, bool add) =>
        SendAsync(MinistryMethods.ChangePerson, new PersonChange { MinistryId = ministryId, PersonId = personId, Role = role, Add = add });

    public Task LinkParentAsync(string ministryId, string childId, string parentId) =>
        SendAsync(MinistryMethods.LinkParent, new LinkParentRequest { MinistryId = ministryId, ChildId = childId, ParentId = parentId });

    public Task SavePostAsync(string ministryId, Post post)
    {
        Post sent = MinistriesContractsModel.Instance.DeepClone(post);
        return SendAsync(MinistryMethods.SavePost, new PostSave { MinistryId = ministryId, Post = Minted(sent, sent.Id, id => (sent.Id, sent.New) = (id, true)) });
    }

    public Task DeletePostAsync(string ministryId, string id) => SendAsync(MinistryMethods.DeletePost, new ItemRef { MinistryId = ministryId, Id = id });

    public Task SaveFileAsync(string ministryId, FileItem file, QueuedUpload? upload = null)
    {
        FileItem sent = MinistriesContractsModel.Instance.DeepClone(file);
        return SendAsync(MinistryMethods.SaveFile, new FileSave { MinistryId = ministryId, File = Minted(sent, sent.Id, id => (sent.Id, sent.New) = (id, true)) }, upload);
    }

    public Task DeleteFileAsync(string ministryId, string id) => SendAsync(MinistryMethods.DeleteFile, new ItemRef { MinistryId = ministryId, Id = id });

    public async Task<MinistryDetails> SaveMinistryAsync(MinistryDetails details)
    {
        await SendAsync(MinistryMethods.SaveMinistry, new MinistrySave { MinistryId = details.Id, Details = details });
        return details;
    }

    public Task SavePartsAsync(string ministryId, IEnumerable<string> partsOff, IEnumerable<string> partsFromBelow, string chatStarters = "",
        IEnumerable<string>? partsOrder = null) =>
        SendAsync(MinistryMethods.SaveParts, new PartsSave
        {
            MinistryId = ministryId, PartsOff = [.. partsOff], PartsFromBelow = [.. partsFromBelow], ChatStarters = chatStarters, PartsOrder = [.. partsOrder ?? []],
        });

    public Task MoveAsync(string ministryId, string underId) => SendAsync(MinistryMethods.Move, new MoveRequest { MinistryId = ministryId, UnderId = underId });

    public async Task<MinistryDetails> AddSubMinistryAsync(string parentId, MinistryDetails details)
    {
        Minted(details, "", id => (details.Id, details.New, details.ParentId, details.PartsOff) = (id, true, parentId, MinistryParts.OffWhenNew()));
        await SendAsync(MinistryMethods.AddSubMinistry, new MinistrySave { MinistryId = parentId, Details = details });
        return details;
    }

    public async Task<MinistryDetails> CreateAsync(MinistryDetails details)
    {
        Minted(details, "", id => (details.Id, details.New, details.PartsOff) = (id, true, MinistryParts.OffWhenNew()));
        await SendAsync(MinistryMethods.Create, new MinistrySave { Details = details });
        return details;
    }

    public Task DeleteAsync(string ministryId) => SendAsync(MinistryMethods.Delete, new MinistryRef { MinistryId = ministryId });

    public Task ArchiveAsync(string ministryId) => SendAsync(MinistryMethods.Archive, new MinistryRef { MinistryId = ministryId });

    public Task SaveCalendarAsync(string ministryId, CalendarLine calendar) => SendAsync(MinistryMethods.SaveCalendar, new CalendarSave { MinistryId = ministryId, Calendar = calendar });

    public Task LinkCalendarAsync(string ministryId, string calendarId, bool linked) =>
        SendAsync(MinistryMethods.LinkCalendar, new CalendarLinkSave { MinistryId = ministryId, CalendarId = calendarId, Linked = linked });

    public Task RemoveCalendarAsync(string ministryId, string id) => SendAsync(MinistryMethods.RemoveCalendar, new ItemRef { MinistryId = ministryId, Id = id });

    public Task SaveSessionAsync(string groupId, SessionChange change) => SendAsync(MinistryMethods.SaveSession, new SessionSave { MinistryId = groupId, Change = change });

    private static T Minted<T>(T item, string id, Action<string> mint)
    {
        if (id.Length == 0)
            mint(Guid.CreateVersion7().ToString());
        return item;
    }

    private async Task SendAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request, QueuedUpload? upload = null)
    {
        using MemoryStream body = new();
        MinistriesContractsModel.Instance.Serialize(body, request);
        await outbox.EnqueueAsync(method, body.ToArray(), upload is null ? [] : [upload]);
    }
}
