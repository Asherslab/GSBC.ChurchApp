using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SermonsImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sermons_import");

            migrationBuilder.CreateTable(
                name: "runs",
                schema: "sermons_import",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    listing_pages = table.Column<int>(type: "integer", nullable: false),
                    website_count = table.Column<int>(type: "integer", nullable: true),
                    current_path = table.Column<string>(type: "text", nullable: true),
                    problem = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sermons",
                schema: "sermons_import",
                columns: table => new
                {
                    website_path = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    preached_on = table.Column<DateOnly>(type: "date", nullable: true),
                    state = table.Column<string>(type: "text", nullable: false),
                    website_id = table.Column<int>(type: "integer", nullable: true),
                    sermon_id = table.Column<Guid>(type: "uuid", nullable: true),
                    problem = table.Column<string>(type: "text", nullable: true),
                    bytes = table.Column<long>(type: "bigint", nullable: false),
                    copied_bytes = table.Column<long>(type: "bigint", nullable: false),
                    seen_in_run = table.Column<int>(type: "integer", nullable: false),
                    imported_in_run = table.Column<int>(type: "integer", nullable: true),
                    already_there = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sermons", x => x.website_path);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sermons_sermon_id",
                schema: "sermons_import",
                table: "sermons",
                column: "sermon_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "runs",
                schema: "sermons_import");

            migrationBuilder.DropTable(
                name: "sermons",
                schema: "sermons_import");
        }
    }
}
