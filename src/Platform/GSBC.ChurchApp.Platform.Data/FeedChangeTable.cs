using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Platform.Data;

public static class FeedChangeTable
{
    public const string Schema = "sync";
    public const string Name = "feed_changes";
    public const string NotifyChannel = "gsbc_feed_changes";

    public static void Map(ModelBuilder modelBuilder, bool owned = false) =>
        modelBuilder.Entity<FeedChangeRecord>(entity =>
        {
            entity.ToTable(Name, Schema, table =>
            {
                if (!owned)
                    table.ExcludeFromMigrations();
            });
            entity.HasKey(change => change.Cursor);
            entity.Property(change => change.Cursor).UseIdentityAlwaysColumn();
            entity.Property(change => change.CommittedAt).HasDefaultValueSql("now()");
            entity.HasIndex(change => new { change.Feed, change.Cursor });
        });
}
