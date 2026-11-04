using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ArtworkSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "photo_hash",
                schema: "sermons",
                table: "speakers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "artwork_hash",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "artwork_hash",
                schema: "sermons",
                table: "series",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "artwork_addresses",
                schema: "sermons",
                columns: table => new
                {
                    url = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    e_tag = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<string>(type: "text", nullable: true),
                    fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_addresses", x => x.url);
                });

            migrationBuilder.CreateTable(
                name: "artwork_sources",
                schema: "sermons",
                columns: table => new
                {
                    hash = table.Column<string>(type: "text", nullable: false),
                    large = table.Column<string>(type: "jsonb", nullable: true),
                    small = table.Column<string>(type: "jsonb", nullable: true),
                    photo = table.Column<string>(type: "jsonb", nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artwork_sources", x => x.hash);
                });

            migrationBuilder.CreateIndex(
                name: "ix_artwork_addresses_hash",
                schema: "sermons",
                table: "artwork_addresses",
                column: "hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artwork_addresses",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "artwork_sources",
                schema: "sermons");

            migrationBuilder.DropColumn(
                name: "photo_hash",
                schema: "sermons",
                table: "speakers");

            migrationBuilder.DropColumn(
                name: "artwork_hash",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "artwork_hash",
                schema: "sermons",
                table: "series");
        }
    }
}
