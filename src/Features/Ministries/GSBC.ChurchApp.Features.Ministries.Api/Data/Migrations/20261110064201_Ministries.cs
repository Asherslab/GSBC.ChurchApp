using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Ministries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ministries");

            migrationBuilder.CreateTable(
                name: "asks",
                schema: "ministries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @as = table.Column<string>(name: "as", type: "text", nullable: false),
                    asked_by = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "files",
                schema: "ministries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    media_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audience = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    added_by = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ministries",
                schema: "ministries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    about = table.Column<string>(type: "text", nullable: false),
                    meets = table.Column<string>(type: "text", nullable: false),
                    who_for = table.Column<string>(type: "text", nullable: false),
                    colour = table.Column<string>(type: "text", nullable: false),
                    icon = table.Column<string>(type: "text", nullable: false),
                    team_join = table.Column<string>(type: "text", nullable: false),
                    attend_join = table.Column<string>(type: "text", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: true),
                    room = table.Column<string>(type: "text", nullable: false),
                    join_as_parent = table.Column<bool>(type: "boolean", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ministries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                schema: "ministries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    audience = table.Column<string>(type: "text", nullable: false),
                    important = table.Column<bool>(type: "boolean", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_name = table.Column<string>(type: "text", nullable: false),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    whole_church_asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rounds",
                schema: "ministries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sundays = table.Column<List<DateOnly>>(type: "date[]", nullable: false),
                    starts = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ends = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    shows_from = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rounds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "ministries",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    off = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => new { x.group_id, x.date });
                });

            migrationBuilder.CreateIndex(
                name: "ix_asks_ministry_id_person_id_as",
                schema: "ministries",
                table: "asks",
                columns: new[] { "ministry_id", "person_id", "as" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asks_person_id",
                schema: "ministries",
                table: "asks",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_files_ministry_id",
                schema: "ministries",
                table: "files",
                column: "ministry_id");

            migrationBuilder.CreateIndex(
                name: "ix_ministries_parent_id",
                schema: "ministries",
                table: "ministries",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_ministry_id_posted_at",
                schema: "ministries",
                table: "posts",
                columns: new[] { "ministry_id", "posted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_rounds_ministry_id",
                schema: "ministries",
                table: "rounds",
                column: "ministry_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asks",
                schema: "ministries");

            migrationBuilder.DropTable(
                name: "files",
                schema: "ministries");

            migrationBuilder.DropTable(
                name: "ministries",
                schema: "ministries");

            migrationBuilder.DropTable(
                name: "posts",
                schema: "ministries");

            migrationBuilder.DropTable(
                name: "rounds",
                schema: "ministries");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "ministries");
        }
    }
}
