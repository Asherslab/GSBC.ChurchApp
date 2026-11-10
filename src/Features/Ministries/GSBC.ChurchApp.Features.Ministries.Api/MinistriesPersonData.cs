using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Platform.Api.People;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistriesPersonData(MinistriesDbContext db) : IPersonData
{
    public async Task ForgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        await db.Asks.Where(ask => ask.PersonId == userId || ask.AskedBy == userId).ExecuteDeleteAsync(cancellationToken);
        await db.Posts.Where(post => post.AuthorId == userId)
            .ExecuteUpdateAsync(set => set.SetProperty(post => post.AuthorId, Guid.Empty).SetProperty(post => post.AuthorName, ""), cancellationToken);
        await db.Files.Where(file => file.AddedBy == userId).ExecuteUpdateAsync(set => set.SetProperty(file => file.AddedBy, Guid.Empty), cancellationToken);
    }
}
