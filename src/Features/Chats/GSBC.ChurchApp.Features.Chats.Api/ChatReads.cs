using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatReads(ChatsDbContext db)
{
    public const int MostMessages = 400;
    private const int MostAtOnce = 10_000;

    public async Task<MyChats> MineAsync(Guid me, CancellationToken cancellationToken)
    {
        List<FeedChangeRecord> rows = await Current(ChatsFeeds.Chats, me).ToListAsync(cancellationToken);
        return new MyChats
        {
            Chats = rows.Where(row => row.GroupKey == ChatRows.CardGroup).Select(row => Decode<ChatCard>(row.Payload)).ToList(),
            Paused = rows.Any(row => row.GroupKey == ChatRows.StatusGroup),
        };
    }

    public async Task<ChatMessages> MessagesAsync(Guid me, MessagesRequest request, CancellationToken cancellationToken)
    {
        int limit = request.Limit > 0 ? Math.Min(request.Limit, MostAtOnce) : MostMessages;
        IQueryable<FeedChangeRecord> rows = Current(ChatsFeeds.Messages, me).Where(row => row.GroupKey == request.ChatId);
        if (request.BeforeSeq > 0)
        {
            string before = ChatRows.SortKey(request.BeforeSeq);
            rows = rows.Where(row => string.Compare(row.SortKey, before) < 0);
        }
        List<FeedChangeRecord> page = await rows.OrderByDescending(row => row.SortKey).Take(limit + 1).ToListAsync(cancellationToken);
        return new ChatMessages
        {
            HasMore = page.Count > limit,
            Messages = page.Take(limit).Select(row => Decode<ChatMessage>(row.Payload)).OrderBy(message => message.Seq).ToList(),
        };
    }

    private IQueryable<FeedChangeRecord> Current(string feed, Guid me)
    {
        string owner = me.ToString();
        return db.FeedChanges.AsNoTracking()
            .Where(row => row.Feed == feed && row.ScopeId == owner)
            .Where(row => !db.FeedChanges.Any(newer => newer.Feed == row.Feed && newer.RecordId == row.RecordId && newer.Cursor > row.Cursor))
            .Where(row => !row.Deleted);
    }

    private static T Decode<T>(byte[] payload) => ChatsContractsModel.Instance.Deserialize<T>(new MemoryStream(payload));
}
