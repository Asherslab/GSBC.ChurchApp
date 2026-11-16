using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Prayer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ForgetGettingInTouch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE prayer.requests SET care = 0 WHERE care = 1;");

            migrationBuilder.Sql("""
                INSERT INTO sync.feed_horizons (feed, cursor)
                SELECT 'prayer.care', MAX(cursor) + 1 FROM sync.feed_changes WHERE feed = 'prayer.care' HAVING COUNT(*) > 0
                ON CONFLICT (feed) DO UPDATE SET cursor = GREATEST(sync.feed_horizons.cursor, EXCLUDED.cursor);
                DELETE FROM sync.feed_changes WHERE feed = 'prayer.care';
                """);

            migrationBuilder.DropColumn(
                name: "contact",
                schema: "prayer",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "wants_contact",
                schema: "prayer",
                table: "requests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contact",
                schema: "prayer",
                table: "requests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "wants_contact",
                schema: "prayer",
                table: "requests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
