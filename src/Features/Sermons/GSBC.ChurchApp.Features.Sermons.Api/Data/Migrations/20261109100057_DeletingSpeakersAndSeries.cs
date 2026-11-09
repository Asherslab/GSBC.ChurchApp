using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeletingSpeakersAndSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "speaker_corrected",
                schema: "sermons",
                table: "sermons",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE sermons.sermons SET speaker_corrected = TRUE WHERE speaker_id IS NOT NULL;");

            migrationBuilder.CreateTable(
                name: "removed_series",
                schema: "sermons",
                columns: table => new
                {
                    slug = table.Column<string>(type: "text", nullable: false),
                    moved_to = table.Column<Guid>(type: "uuid", nullable: true),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_removed_series", x => x.slug);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "removed_series",
                schema: "sermons");

            migrationBuilder.DropColumn(
                name: "speaker_corrected",
                schema: "sermons",
                table: "sermons");
        }
    }
}
