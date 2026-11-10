using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class HandledRequests(EventsDbContext db, TimeProvider time)
{
    public static readonly TimeSpan KeptFor = TimeSpan.FromDays(7);

    public async Task<bool> AlreadyAsync(CallContext context)
    {
        if (QueuedRequest.IdOf(context) is not { } id)
            return false;
        if (await db.HandledRequests.AsNoTracking().AnyAsync(handled => handled.Id == id, context.CancellationToken))
            return true;
        db.HandledRequests.Add(new HandledRequestRecord { Id = id, HandledAt = time.GetUtcNow().UtcDateTime });
        return false;
    }

    public async Task ForgetOldAsync(CancellationToken cancellationToken)
    {
        DateTime before = time.GetUtcNow().UtcDateTime - KeptFor;
        await db.HandledRequests.Where(handled => handled.HandledAt < before).ExecuteDeleteAsync(cancellationToken);
    }
}
