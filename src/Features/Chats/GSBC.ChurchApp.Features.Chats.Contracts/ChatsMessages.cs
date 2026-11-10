using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Chats.Contracts;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class PersonRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class ChatPerson
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Role { get; set; } = "";
    [ProtoMember(4)] public string State { get; set; } = "";
    [ProtoMember(5)] public bool Minor { get; set; }
    [ProtoMember(6)] public bool BlueCard { get; set; }
    [ProtoMember(7)] public long ReadSeq { get; set; }
    [ProtoMember(8)] public List<string> ParentOf { get; set; } = [];
}

[ProtoContract]
public sealed class ChatCard
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string MinistryId { get; set; } = "";
    [ProtoMember(3)] public string MinistryName { get; set; } = "";
    [ProtoMember(4)] public string Name { get; set; } = "";
    [ProtoMember(5)] public string Kind { get; set; } = "";
    [ProtoMember(6)] public string Audience { get; set; } = "";
    [ProtoMember(7)] public string YourRole { get; set; } = "";
    [ProtoMember(8)] public bool YouMaySend { get; set; }
    [ProtoMember(9)] public bool YouMayManage { get; set; }
    [ProtoMember(10)] public List<PersonRef> ForChildren { get; set; } = [];
    [ProtoMember(11)] public bool ParentsCanRead { get; set; }
    [ProtoMember(12)] public bool Muted { get; set; }
    [ProtoMember(13)] public long LastSeq { get; set; }
    [ProtoMember(14)] public string LastPreview { get; set; } = "";
    [ProtoMember(15)] public DateTime? LastAt { get; set; }
    [ProtoMember(16)] public string LastSender { get; set; } = "";
    [ProtoMember(17)] public long ReadSeq { get; set; }
    [ProtoMember(18)] public List<ChatPerson> People { get; set; } = [];
    [ProtoMember(19)] public List<PersonRef> AwaitingYourConsent { get; set; } = [];
    [ProtoMember(20)] public List<string> Flags { get; set; } = [];
    [ProtoMember(21)] public DateTime CreatedAt { get; set; }
    [ProtoMember(22)] public PersonRef? Asker { get; set; }
    [ProtoMember(23)] public bool HasMinors { get; set; }
    [ProtoMember(24)] public long FirstSeq { get; set; }
    [ProtoMember(25)] public string You { get; set; } = "";
}

[ProtoContract]
public sealed class ChatsStatus
{
    [ProtoMember(1)] public bool Paused { get; set; }
    [ProtoMember(2)] public DateTime? PausedAt { get; set; }
}

[ProtoContract]
public sealed class Quote
{
    [ProtoMember(1)] public string MessageId { get; set; } = "";
    [ProtoMember(2)] public string SenderName { get; set; } = "";
    [ProtoMember(3)] public string Snippet { get; set; } = "";
}

[ProtoContract]
public sealed class Reaction
{
    [ProtoMember(1)] public string Emoji { get; set; } = "";
    [ProtoMember(2)] public List<string> People { get; set; } = [];
}

[ProtoContract]
public sealed class Attachment
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string ContentType { get; set; } = "";
    [ProtoMember(4)] public long Bytes { get; set; }
    [ProtoMember(5)] public int Width { get; set; }
    [ProtoMember(6)] public int Height { get; set; }
}

[ProtoContract]
public sealed class ChatMessage
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string ChatId { get; set; } = "";
    [ProtoMember(3)] public long Seq { get; set; }
    [ProtoMember(4)] public string Kind { get; set; } = "";
    [ProtoMember(5)] public string SenderId { get; set; } = "";
    [ProtoMember(6)] public string SenderName { get; set; } = "";
    [ProtoMember(7)] public string ForName { get; set; } = "";
    [ProtoMember(8)] public DateTime SentAt { get; set; }
    [ProtoMember(9)] public string Text { get; set; } = "";
    [ProtoMember(10)] public Quote? ReplyTo { get; set; }
    [ProtoMember(11)] public List<string> Mentions { get; set; } = [];
    [ProtoMember(12)] public List<Reaction> Reactions { get; set; } = [];
    [ProtoMember(13)] public List<Attachment> Attachments { get; set; } = [];
    [ProtoMember(14)] public DateTime? EditedAt { get; set; }
    [ProtoMember(15)] public bool Deleted { get; set; }
    [ProtoMember(16)] public string ClientId { get; set; } = "";
}

[ProtoContract]
public sealed class StartChat : IScopedRequest
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public string MinistryId { get; set; } = "";
    [ProtoMember(3)] public string Name { get; set; } = "";
    [ProtoMember(4)] public string Kind { get; set; } = "";
    [ProtoMember(5)] public string Audience { get; set; } = "";
    [ProtoMember(6)] public List<string> People { get; set; } = [];
    [ProtoMember(7)] public string FirstMessage { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class ChangeChat
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Audience { get; set; } = "";
}

[ProtoContract]
public sealed class ChatRef
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
}

[ProtoContract]
public sealed class ChatPeopleChange
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public List<string> Add { get; set; } = [];
    [ProtoMember(3)] public List<string> Remove { get; set; } = [];
}

[ProtoContract]
public sealed class SendMessage
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public string ClientId { get; set; } = "";
    [ProtoMember(3)] public string Text { get; set; } = "";
    [ProtoMember(4)] public string ReplyToId { get; set; } = "";
    [ProtoMember(5)] public List<string> Mentions { get; set; } = [];
    [ProtoMember(6)] public List<Attachment> Attachments { get; set; } = [];
    [ProtoMember(7)] public string ForPersonId { get; set; } = "";
}

[ProtoContract]
public sealed class EditMessage
{
    [ProtoMember(1)] public string MessageId { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
}

[ProtoContract]
public sealed class MessageRef
{
    [ProtoMember(1)] public string MessageId { get; set; } = "";
}

[ProtoContract]
public sealed class React
{
    [ProtoMember(1)] public string MessageId { get; set; } = "";
    [ProtoMember(2)] public string Emoji { get; set; } = "";
    [ProtoMember(3)] public bool On { get; set; }
}

[ProtoContract]
public sealed class MarkRead
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public long UpToSeq { get; set; }
}

[ProtoContract]
public sealed class Mute
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public bool Muted { get; set; }
}

[ProtoContract]
public sealed class Consent
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public string ChildId { get; set; } = "";
    [ProtoMember(3)] public bool Approve { get; set; }
}

[ProtoContract]
public sealed class ReportMessage
{
    [ProtoMember(1)] public string MessageId { get; set; } = "";
    [ProtoMember(2)] public string To { get; set; } = "";
    [ProtoMember(3)] public string Note { get; set; } = "";
    [ProtoMember(4)] public string ReportId { get; set; } = "";
}

[ProtoContract]
public sealed class AskToJoin : IScopedRequest
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public string MinistryId { get; set; } = "";
    [ProtoMember(3)] public string ForPersonId { get; set; } = "";
    [ProtoMember(4)] public string Text { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class MinistryChatsRequest : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class DeskRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
}

[ProtoContract]
public sealed class DeskChat
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string MinistryId { get; set; } = "";
    [ProtoMember(3)] public string MinistryName { get; set; } = "";
    [ProtoMember(4)] public string Name { get; set; } = "";
    [ProtoMember(5)] public string Kind { get; set; } = "";
    [ProtoMember(6)] public string Audience { get; set; } = "";
    [ProtoMember(7)] public int People { get; set; }
    [ProtoMember(8)] public int Messages { get; set; }
    [ProtoMember(9)] public DateTime? LastAt { get; set; }
    [ProtoMember(10)] public List<string> Flags { get; set; } = [];
    [ProtoMember(11)] public bool MayManage { get; set; }
    [ProtoMember(12)] public int OpenReports { get; set; }
    [ProtoMember(13)] public bool HasMinors { get; set; }
}

[ProtoContract]
public sealed class Desk
{
    [ProtoMember(1)] public List<DeskChat> Chats { get; set; } = [];
    [ProtoMember(2)] public bool Auditor { get; set; }
    [ProtoMember(3)] public int OpenReports { get; set; }
    [ProtoMember(4)] public List<DeskMinistry> Ministries { get; set; } = [];
}

[ProtoContract]
public sealed class DeskMinistry
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public bool MayManage { get; set; }
    [ProtoMember(4)] public bool ChatsOn { get; set; }
    [ProtoMember(5)] public List<string> Audiences { get; set; } = [];
}

[ProtoContract]
public sealed class CheckRequest
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public string Reason { get; set; } = "";
}

[ProtoContract]
public sealed class MessageVersion
{
    [ProtoMember(1)] public int Number { get; set; }
    [ProtoMember(2)] public string Action { get; set; } = "";
    [ProtoMember(3)] public string Text { get; set; } = "";
    [ProtoMember(4)] public DateTime At { get; set; }
    [ProtoMember(5)] public string ByName { get; set; } = "";
    [ProtoMember(6)] public List<Attachment> Attachments { get; set; } = [];
}

[ProtoContract]
public sealed class CheckedMessage
{
    [ProtoMember(1, IsRequired = true)] public ChatMessage Message { get; set; } = new();
    [ProtoMember(2)] public List<MessageVersion> Versions { get; set; } = [];
    [ProtoMember(3)] public string DeletedText { get; set; } = "";
}

[ProtoContract]
public sealed class CheckedChat
{
    [ProtoMember(1, IsRequired = true)] public DeskChat Chat { get; set; } = new();
    [ProtoMember(2)] public List<ChatPerson> People { get; set; } = [];
    [ProtoMember(3)] public List<CheckedMessage> Messages { get; set; } = [];
    [ProtoMember(4)] public List<LogLine> Checks { get; set; } = [];
}

[ProtoContract]
public sealed class LogLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public DateTime At { get; set; }
    [ProtoMember(3)] public string ActorName { get; set; } = "";
    [ProtoMember(4)] public string Action { get; set; } = "";
    [ProtoMember(5)] public string Words { get; set; } = "";
    [ProtoMember(6)] public string ChatId { get; set; } = "";
    [ProtoMember(7)] public string ChatName { get; set; } = "";
}

[ProtoContract]
public sealed class LogRequest
{
    [ProtoMember(1)] public string ChatId { get; set; } = "";
    [ProtoMember(2)] public int Limit { get; set; }
}

[ProtoContract]
public sealed class Log
{
    [ProtoMember(1)] public List<LogLine> Lines { get; set; } = [];
}

[ProtoContract]
public sealed class ReportLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string ChatId { get; set; } = "";
    [ProtoMember(3)] public string ChatName { get; set; } = "";
    [ProtoMember(4)] public string MinistryName { get; set; } = "";
    [ProtoMember(5)] public string To { get; set; } = "";
    [ProtoMember(6)] public string ReporterName { get; set; } = "";
    [ProtoMember(7)] public string Note { get; set; } = "";
    [ProtoMember(8)] public DateTime At { get; set; }
    [ProtoMember(9)] public string MessageId { get; set; } = "";
    [ProtoMember(10)] public string MessageSender { get; set; } = "";
    [ProtoMember(11)] public string MessageSenderId { get; set; } = "";
    [ProtoMember(12)] public string MessageText { get; set; } = "";
    [ProtoMember(13)] public DateTime? HandledAt { get; set; }
    [ProtoMember(14)] public string HandledBy { get; set; } = "";
    [ProtoMember(15)] public string Outcome { get; set; } = "";
}

[ProtoContract]
public sealed class Reports
{
    [ProtoMember(1)] public List<ReportLine> Open { get; set; } = [];
    [ProtoMember(2)] public List<ReportLine> Handled { get; set; } = [];
}

[ProtoContract]
public sealed class HandleReport
{
    [ProtoMember(1)] public string ReportId { get; set; } = "";
    [ProtoMember(2)] public string Outcome { get; set; } = "";
}

[ProtoContract]
public sealed class PauseRequest
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
    [ProtoMember(2)] public string Reason { get; set; } = "";
    [ProtoMember(3)] public bool Paused { get; set; }
}

[ProtoContract]
public sealed class PausedPerson
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public DateTime? Since { get; set; }
    [ProtoMember(4)] public string Reason { get; set; } = "";
    [ProtoMember(5)] public string ByName { get; set; } = "";
}

[ProtoContract]
public sealed class PausedList
{
    [ProtoMember(1)] public List<PausedPerson> People { get; set; } = [];
}

[ProtoContract]
public sealed class Nothing;

[ProtoContract]
public sealed class PendingSend
{
    [ProtoMember(1)] public string OutboxId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public SendMessage Message { get; set; } = new();
    [ProtoMember(3)] public DateTime At { get; set; }
    [ProtoMember(4)] public string ReplyName { get; set; } = "";
    [ProtoMember(5)] public string ReplySnippet { get; set; } = "";
}

[ProtoContract]
public sealed class PendingSends
{
    [ProtoMember(1)] public List<PendingSend> Sends { get; set; } = [];
}
