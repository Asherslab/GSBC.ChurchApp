using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OneOriginalPerPictureLastingIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thumbnail",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "thumbnail",
                schema: "sermons",
                table: "series");

            migrationBuilder.AddColumn<string>(
                name: "picture",
                schema: "sermons",
                table: "artwork_sources",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql("UPDATE sermons.artwork_sources SET picture = coalesce(large, photo);");

            migrationBuilder.DropColumn(
                name: "large",
                schema: "sermons",
                table: "artwork_sources");

            migrationBuilder.DropColumn(
                name: "small",
                schema: "sermons",
                table: "artwork_sources");

            migrationBuilder.DropColumn(
                name: "photo",
                schema: "sermons",
                table: "artwork_sources");

            migrationBuilder.CreateTable(
                name: "former_ids",
                schema: "sermons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    moved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    settled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_former_ids", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_former_ids_current_id",
                schema: "sermons",
                table: "former_ids",
                column: "current_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "former_ids",
                schema: "sermons");

            migrationBuilder.AddColumn<string>(
                name: "thumbnail",
                schema: "sermons",
                table: "sermons",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail",
                schema: "sermons",
                table: "series",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "large",
                schema: "sermons",
                table: "artwork_sources",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo",
                schema: "sermons",
                table: "artwork_sources",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "small",
                schema: "sermons",
                table: "artwork_sources",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql("UPDATE sermons.artwork_sources SET large = picture;");

            migrationBuilder.DropColumn(
                name: "picture",
                schema: "sermons",
                table: "artwork_sources");
        }
    }
}
