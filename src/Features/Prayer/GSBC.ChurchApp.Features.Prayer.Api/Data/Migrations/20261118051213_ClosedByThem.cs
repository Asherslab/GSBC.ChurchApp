using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Prayer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClosedByThem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "message",
                schema: "prayer",
                table: "requests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE prayer.requests r
                SET care = 3, message = r.answered_note, answered_note = ''
                WHERE r.care = 2
                  AND (SELECT s.what FROM prayer.steps s
                       WHERE s.request_id = r.id AND s.what IN ('They said it was answered', 'Marked answered', 'Marked new again')
                       ORDER BY s.at DESC, s.id DESC LIMIT 1) = 'They said it was answered';

                UPDATE prayer.steps s
                SET what = CASE WHEN r.message = '' THEN 'They closed the request' ELSE 'They closed the request and left a message' END
                FROM prayer.requests r
                WHERE s.request_id = r.id AND r.care = 3 AND s.what = 'They said it was answered';

                UPDATE prayer.steps s
                SET what = replace(s.what, 'Shows their thanks', 'Shows their message')
                FROM prayer.requests r
                WHERE s.request_id = r.id AND r.care = 3 AND s.what LIKE 'Shows their thanks on the prayer wall until %';
                """);

            migrationBuilder.Sql("""
                INSERT INTO sync.feed_horizons (feed, cursor)
                SELECT feed, MAX(cursor) + 1 FROM sync.feed_changes
                WHERE feed IN ('prayer.wall', 'prayer.mine', 'prayer.approving', 'prayer.care')
                GROUP BY feed
                ON CONFLICT (feed) DO UPDATE SET cursor = GREATEST(sync.feed_horizons.cursor, EXCLUDED.cursor);
                DELETE FROM sync.feed_changes WHERE feed IN ('prayer.wall', 'prayer.mine', 'prayer.approving', 'prayer.care');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE prayer.requests SET care = 2, answered_note = CASE WHEN answered_note = '' THEN message ELSE answered_note END WHERE care = 3;");

            migrationBuilder.DropColumn(
                name: "message",
                schema: "prayer",
                table: "requests");
        }
    }
}
