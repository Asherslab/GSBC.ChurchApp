using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SundayPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "no_class",
                schema: "ministries",
                table: "posts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "sunday",
                schema: "ministries",
                table: "posts",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO ministries.posts (id, ministry_id, title, body, audience, important, author_id, author_name, posted_at, sunday, no_class)
                SELECT gen_random_uuid(), group_id,
                       CASE WHEN off THEN 'No class this Sunday' ELSE left(note, 120) END,
                       CASE WHEN off THEN note ELSE '' END,
                       'everyone', false, '00000000-0000-0000-0000-000000000000', '', now(), date, off
                FROM ministries.sessions
                """);

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "ministries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "no_class",
                schema: "ministries",
                table: "posts");

            migrationBuilder.DropColumn(
                name: "sunday",
                schema: "ministries",
                table: "posts");

            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "ministries",
                columns: table => new
                {
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    off = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sessions", x => new { x.group_id, x.date });
                });
        }
    }
}
