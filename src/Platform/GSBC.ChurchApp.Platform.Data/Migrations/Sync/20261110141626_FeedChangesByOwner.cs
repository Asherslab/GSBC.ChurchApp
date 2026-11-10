using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Platform.Data.Migrations.Sync
{
    /// <inheritdoc />
    public partial class FeedChangesByOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_feed_changes_feed_scope_id_cursor",
                schema: "sync",
                table: "feed_changes",
                columns: new[] { "feed", "scope_id", "cursor" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_feed_changes_feed_scope_id_cursor",
                schema: "sync",
                table: "feed_changes");
        }
    }
}
