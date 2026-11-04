using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GSBC.ChurchApp.Platform.Data.Migrations.Sync
{
    /// <inheritdoc />
    public partial class InitialSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sync");

            migrationBuilder.CreateTable(
                name: "feed_changes",
                schema: "sync",
                columns: table => new
                {
                    cursor = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    feed = table.Column<string>(type: "text", nullable: false),
                    record_id = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    deleted = table.Column<bool>(type: "boolean", nullable: false),
                    payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    sort_key = table.Column<string>(type: "text", nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    group_key = table.Column<string>(type: "text", nullable: true),
                    scope_id = table.Column<string>(type: "text", nullable: true),
                    committed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feed_changes", x => x.cursor);
                });

            migrationBuilder.CreateIndex(
                name: "ix_feed_changes_feed_cursor",
                schema: "sync",
                table: "feed_changes",
                columns: new[] { "feed", "cursor" });

            migrationBuilder.Sql($$"""
                CREATE FUNCTION sync.notify_feed_change() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM pg_notify('{{FeedChangeTable.NotifyChannel}}', NEW.feed || ':' || NEW.cursor);
                    RETURN NEW;
                END $$;

                CREATE TRIGGER feed_change_notify AFTER INSERT ON sync.feed_changes
                    FOR EACH ROW EXECUTE FUNCTION sync.notify_feed_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER feed_change_notify ON sync.feed_changes;
                DROP FUNCTION sync.notify_feed_change();
                """);

            migrationBuilder.DropTable(
                name: "feed_changes",
                schema: "sync");
        }
    }
}
