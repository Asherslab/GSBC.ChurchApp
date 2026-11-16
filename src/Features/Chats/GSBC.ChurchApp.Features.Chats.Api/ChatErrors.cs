using Grpc.Core;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal static class ChatErrors
{
    public static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    public static RpcException Refused(string message) => new(new Status(StatusCode.PermissionDenied, message));

    public static RpcException Gone(string message) => new(new Status(StatusCode.NotFound, message));

    public static string NoRoleToStart(string ministry, bool teamMayStart) =>
        $"Only {ministry}'s {(teamMayStart ? "leaders and team" : "leaders")} start its chats, because they're the people in them. To start one, be given a role in {ministry}.";

    public static Guid IdOf(string text, string what) => Guid.TryParse(text, out Guid id) ? id : throw Gone($"That {what} isn't here any more.");

    public const string Paused = "Your chats are paused for now. Talk to one of your ministry's leaders or the church office.";
    public const string NotInChat = "You're not in that chat.";
    public const string ChatGone = "That chat was deleted.";
    public const string ChatsOff = "This ministry doesn't use chats.";
}
