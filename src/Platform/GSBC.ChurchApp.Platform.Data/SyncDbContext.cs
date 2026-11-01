using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Platform.Data;

public sealed class SyncDbContext(DbContextOptions<SyncDbContext> options) : FeatureDbContext(options)
{
    protected override string Schema => FeedChangeTable.Schema;

    protected override bool OwnsFeedChanges => true;
}
