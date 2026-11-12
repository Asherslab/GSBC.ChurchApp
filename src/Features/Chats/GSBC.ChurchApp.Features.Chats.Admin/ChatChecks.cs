using GSBC.ChurchApp.Features.Chats.Client;
using GSBC.ChurchApp.Features.Chats.Contracts;

namespace GSBC.ChurchApp.Features.Chats.Admin;

public sealed record OpenCheck(CheckedChat Chat, string Reason);

public sealed class ChatChecks(OversightClient oversight)
{
    private readonly Dictionary<string, OpenCheck> _open = [];

    public OpenCheck? Of(string chatId) => _open.GetValueOrDefault(chatId);

    public async Task<OpenCheck> CheckAsync(string chatId, string reason)
    {
        OpenCheck check = new(await oversight.CheckAsync(chatId, reason), reason.Trim());
        _open[chatId] = check;
        return check;
    }

    public static string PageOf(string chatId, string? message = null) =>
        message is null ? $"chats/check/{chatId}" : $"chats/check/{chatId}?message={message}#m-{message}";
}
