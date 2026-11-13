using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.News.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChurchNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "news");

            migrationBuilder.CreateTable(
                name: "posts",
                schema: "news",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "jsonb", nullable: false),
                    audience = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    publish_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    link_path = table.Column<string>(type: "text", nullable: false),
                    link_label = table.Column<string>(type: "text", nullable: false),
                    tell = table.Column<bool>(type: "boolean", nullable: false),
                    tell_title = table.Column<string>(type: "text", nullable: false),
                    tell_body = table.Column<string>(type: "text", nullable: false),
                    urgent = table.Column<bool>(type: "boolean", nullable: false),
                    told_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    source_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_name = table.Column<string>(type: "text", nullable: false),
                    from_link = table.Column<string>(type: "text", nullable: false),
                    asked_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    asked_by_name = table.Column<string>(type: "text", nullable: false),
                    answered_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    answered_by_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_posts_asked_by_id",
                schema: "news",
                table: "posts",
                column: "asked_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_author_id",
                schema: "news",
                table: "posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_posts_source_source_id",
                schema: "news",
                table: "posts",
                columns: new[] { "source", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_posts_state_publish_at",
                schema: "news",
                table: "posts",
                columns: new[] { "state", "publish_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "posts",
                schema: "news");
        }
    }
}
