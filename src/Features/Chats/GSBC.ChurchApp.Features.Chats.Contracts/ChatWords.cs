namespace GSBC.ChurchApp.Features.Chats.Contracts;

public static class ChatsParts
{
    public const string Key = "chats";
}

public static class ChatKinds
{
    public const string Group = "group";
    public const string Channel = "channel";
    public const string Ask = "ask";

    public static bool IsValid(string kind) => kind is Group or Channel or Ask;
}

public static class ChatRoles
{
    public const string Member = "member";
    public const string Leader = "leader";
    public const string ParentReader = "parent";
}

public static class ChatStates
{
    public const string Active = "active";
    public const string AwaitingConsent = "awaiting-consent";
}

public static class ReportRoutes
{
    public const string Leaders = "leaders";
    public const string Auditors = "auditors";

    public static bool IsValid(string route) => route is Leaders or Auditors;
}

public static class MessageKinds
{
    public const string Text = "text";
    public const string Event = "event";
}

public static class ChatLimits
{
    public const int NameLength = 60;
    public const int TextLength = 4000;
    public const int ReactionLength = 16;
    public const int NoteLength = 1000;
    public const int AttachmentsPerMessage = 10;
}

public static class ChatCopy
{
    public const string StaffCanCheck = "Church staff can check messages in this chat.";
    public const string DeletedAccount = "Deleted account";
    public const string DeletedMessage = "This message was deleted.";

    public static string AskToJoin(string ministry) => $"I'd like to chat about joining {ministry}.";
}
