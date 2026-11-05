using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class WholeArtworkCopyProgressKeptAudio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "audio_media",
                schema: "sermons",
                table: "sermons",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "copied_bytes",
                schema: "sermons",
                table: "sermons",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "copy_problem",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "copy_started_at",
                schema: "sermons",
                table: "sermons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "feed_artwork_url",
                schema: "sermons",
                table: "sermons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "page_read_at",
                schema: "sermons",
                table: "sermons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE sermons.sermons SET feed_artwork_url = artwork_source_url WHERE origin = 'website';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "audio_media",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "copied_bytes",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "copy_problem",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "copy_started_at",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "feed_artwork_url",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "page_read_at",
                schema: "sermons",
                table: "sermons");
        }
    }
}
