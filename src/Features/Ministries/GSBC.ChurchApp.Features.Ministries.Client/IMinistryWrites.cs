using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Uploads;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public interface IMinistryWrites
{
    Task<JoinOutcome> JoinAsync(MinistryDetails ministry, string personId, string place, string note, bool parentFirst = false);

    Task ParentAnswerAsync(string ministryId, string askId, bool yes);

    Task LeaveAsync(string ministryId, string personId, string place);

    Task WithdrawAsync(string ministryId, string askId);

    Task AnswerAsync(string ministryId, string askId, bool yes);

    Task ChangePersonAsync(string ministryId, string personId, string role, bool add);

    Task LinkParentAsync(string ministryId, string childId, string parentId);

    Task SavePostAsync(string ministryId, Post post);

    Task DeletePostAsync(string ministryId, string id);

    Task SaveFileAsync(string ministryId, FileItem file, QueuedUpload? upload = null);

    Task DeleteFileAsync(string ministryId, string id);

    Task<MinistryDetails> SaveMinistryAsync(MinistryDetails details);

    Task SavePartsAsync(string ministryId, IEnumerable<string> partsOff, IEnumerable<string> partsFromBelow, string chatStarters = "", IEnumerable<string>? partsOrder = null);

    Task MoveAsync(string ministryId, string underId);

    Task<MinistryDetails> AddSubMinistryAsync(string parentId, MinistryDetails details);

    Task<MinistryDetails> CreateAsync(MinistryDetails details);

    Task DeleteAsync(string ministryId);

    Task ArchiveAsync(string ministryId);

    Task SaveCalendarAsync(string ministryId, CalendarLine calendar);

    Task RemoveCalendarAsync(string ministryId, string id);

    Task SaveSessionAsync(string groupId, SessionChange change);
}
