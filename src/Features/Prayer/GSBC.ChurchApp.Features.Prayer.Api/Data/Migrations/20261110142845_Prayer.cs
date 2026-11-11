using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GSBC.ChurchApp.Features.Prayer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Prayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "prayer");

            migrationBuilder.CreateTable(
                name: "requests",
                schema: "prayer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    contact = table.Column<string>(type: "text", nullable: false),
                    wants_contact = table.Column<bool>(type: "boolean", nullable: false),
                    minor = table.Column<bool>(type: "boolean", nullable: false),
                    show_name = table.Column<bool>(type: "boolean", nullable: false),
                    care = table.Column<int>(type: "integer", nullable: false),
                    answered_note = table.Column<string>(type: "text", nullable: false),
                    wall = table.Column<int>(type: "integer", nullable: false),
                    wall_text = table.Column<string>(type: "text", nullable: false),
                    wall_shows_name = table.Column<bool>(type: "boolean", nullable: false),
                    wall_shown_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    wall_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    prayed = table.Column<int>(type: "integer", nullable: false),
                    prayed_told_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "prayer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_notes_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "prayer",
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prayed",
                schema: "prayer",
                columns: table => new
                {
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    install_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prayed", x => new { x.request_id, x.install_id });
                    table.ForeignKey(
                        name: "fk_prayed_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "prayer",
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "steps",
                schema: "prayer",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    who_id = table.Column<Guid>(type: "uuid", nullable: true),
                    what = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_steps_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "prayer",
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notes_request_id",
                schema: "prayer",
                table: "notes",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_prayed_install_id_at",
                schema: "prayer",
                table: "prayed",
                columns: new[] { "install_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_prayed_user_id",
                schema: "prayer",
                table: "prayed",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_changed_at",
                schema: "prayer",
                table: "requests",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "ix_requests_install_id_sent_at",
                schema: "prayer",
                table: "requests",
                columns: new[] { "install_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_requests_user_id",
                schema: "prayer",
                table: "requests",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_steps_request_id",
                schema: "prayer",
                table: "steps",
                column: "request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notes",
                schema: "prayer");

            migrationBuilder.DropTable(
                name: "prayed",
                schema: "prayer");

            migrationBuilder.DropTable(
                name: "steps",
                schema: "prayer");

            migrationBuilder.DropTable(
                name: "requests",
                schema: "prayer");
        }
    }
}
