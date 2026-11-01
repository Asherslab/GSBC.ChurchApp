using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Platform.Data.Migrations.Sync
{
    /// <inheritdoc />
    public partial class FeedHorizons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feed_horizons",
                schema: "sync",
                columns: table => new
                {
                    feed = table.Column<string>(type: "text", nullable: false),
                    cursor = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feed_horizons", x => x.feed);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feed_horizons",
                schema: "sync");
        }
    }
}
