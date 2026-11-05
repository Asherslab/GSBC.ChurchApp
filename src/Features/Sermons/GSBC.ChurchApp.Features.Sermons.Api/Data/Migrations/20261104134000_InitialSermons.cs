using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSermons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sermons");

            migrationBuilder.CreateTable(
                name: "progress",
                schema: "sermons",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sermon_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<double>(type: "double precision", nullable: false),
                    duration = table.Column<double>(type: "double precision", nullable: false),
                    heard_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    watched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cleared_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progress", x => new { x.user_id, x.sermon_id });
                });

            migrationBuilder.CreateTable(
                name: "series",
                schema: "sermons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    source_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    artwork_source_url = table.Column<string>(type: "text", nullable: true),
                    artwork = table.Column<string>(type: "jsonb", nullable: true),
                    thumbnail = table.Column<string>(type: "jsonb", nullable: true),
                    chosen_artwork = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_series", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sermons",
                schema: "sermons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    origin = table.Column<string>(type: "text", nullable: false),
                    external_id = table.Column<string>(type: "text", nullable: true),
                    web_link = table.Column<string>(type: "text", nullable: true),
                    preached_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_title = table.Column<string>(type: "text", nullable: false),
                    source_speaker = table.Column<string>(type: "text", nullable: false),
                    source_speaker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_service = table.Column<string>(type: "text", nullable: false),
                    source_you_tube_id = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    speaker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    series_corrected = table.Column<bool>(type: "boolean", nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service = table.Column<string>(type: "text", nullable: true),
                    you_tube_corrected = table.Column<bool>(type: "boolean", nullable: false),
                    you_tube_id = table.Column<string>(type: "text", nullable: true),
                    hidden = table.Column<bool>(type: "boolean", nullable: false),
                    audio_url = table.Column<string>(type: "text", nullable: false),
                    audio_type = table.Column<string>(type: "text", nullable: false),
                    audio_bytes = table.Column<long>(type: "bigint", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    audio_copied_from = table.Column<string>(type: "text", nullable: true),
                    artwork_source_url = table.Column<string>(type: "text", nullable: true),
                    artwork = table.Column<string>(type: "jsonb", nullable: true),
                    thumbnail = table.Column<string>(type: "jsonb", nullable: true),
                    published_hash = table.Column<string>(type: "text", nullable: true),
                    announced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sermons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_state",
                schema: "sermons",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    e_tag = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<string>(type: "text", nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    problem = table.Column<string>(type: "text", nullable: true),
                    series_checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    series_problem = table.Column<string>(type: "text", nullable: true),
                    has_read_once = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_state", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "speaker_aliases",
                schema: "sermons",
                columns: table => new
                {
                    alias = table.Column<string>(type: "text", nullable: false),
                    speaker_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_speaker_aliases", x => x.alias);
                });

            migrationBuilder.CreateTable(
                name: "speakers",
                schema: "sermons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    photo_source_url = table.Column<string>(type: "text", nullable: true),
                    photo = table.Column<string>(type: "jsonb", nullable: true),
                    chosen_photo = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_speakers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_series_slug",
                schema: "sermons",
                table: "series",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sermons_external_id",
                schema: "sermons",
                table: "sermons",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sermons_preached_at",
                schema: "sermons",
                table: "sermons",
                column: "preached_at");

            migrationBuilder.CreateIndex(
                name: "ix_speaker_aliases_speaker_id",
                schema: "sermons",
                table: "speaker_aliases",
                column: "speaker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "progress",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "series",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "sermons",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "source_state",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "speaker_aliases",
                schema: "sermons");

            migrationBuilder.DropTable(
                name: "speakers",
                schema: "sermons");
        }
    }
}
