using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GSBC.ChurchApp.Features.Feedback.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Feedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "feedback");

            migrationBuilder.CreateTable(
                name: "blocks",
                schema: "feedback",
                columns: table => new
                {
                    install_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    blocked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    blocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blocks", x => x.install_id);
                });

            migrationBuilder.CreateTable(
                name: "handled_requests",
                schema: "feedback",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    handled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_handled_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tickets",
                schema: "feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    text = table.Column<string>(type: "text", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sender_name = table.Column<string>(type: "text", nullable: false),
                    app_version = table.Column<string>(type: "text", nullable: false),
                    app_build = table.Column<string>(type: "text", nullable: false),
                    platform = table.Column<string>(type: "text", nullable: false),
                    os_version = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    development_build = table.Column<bool>(type: "boolean", nullable: false),
                    environment = table.Column<string>(type: "text", nullable: false),
                    page = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    phone_data = table.Column<byte[]>(type: "bytea", nullable: true),
                    had_phone_data = table.Column<bool>(type: "boolean", nullable: false),
                    attachments_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "upload_asks",
                schema: "feedback",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    install_id = table.Column<Guid>(type: "uuid", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_upload_asks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "feedback",
                columns: table => new
                {
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    media_id = table.Column<string>(type: "text", nullable: false),
                    media = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => new { x.ticket_id, x.position });
                    table.ForeignKey(
                        name: "fk_attachments_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalSchema: "feedback",
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notes",
                schema: "feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: true),
                    text = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_notes_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalSchema: "feedback",
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "steps",
                schema: "feedback",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    who_id = table.Column<Guid>(type: "uuid", nullable: true),
                    what = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_steps_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalSchema: "feedback",
                        principalTable: "tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_handled_requests_handled_at",
                schema: "feedback",
                table: "handled_requests",
                column: "handled_at");

            migrationBuilder.CreateIndex(
                name: "ix_notes_author_id",
                schema: "feedback",
                table: "notes",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_notes_ticket_id",
                schema: "feedback",
                table: "notes",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_steps_ticket_id",
                schema: "feedback",
                table: "steps",
                column: "ticket_id");

            migrationBuilder.CreateIndex(
                name: "ix_steps_who_id",
                schema: "feedback",
                table: "steps",
                column: "who_id");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_closed_at",
                schema: "feedback",
                table: "tickets",
                column: "closed_at");

            migrationBuilder.CreateIndex(
                name: "ix_tickets_install_id_sent_at",
                schema: "feedback",
                table: "tickets",
                columns: new[] { "install_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tickets_number",
                schema: "feedback",
                table: "tickets",
                column: "number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tickets_user_id_sent_at",
                schema: "feedback",
                table: "tickets",
                columns: new[] { "user_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_upload_asks_install_id_at",
                schema: "feedback",
                table: "upload_asks",
                columns: new[] { "install_id", "at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachments",
                schema: "feedback");

            migrationBuilder.DropTable(
                name: "blocks",
                schema: "feedback");

            migrationBuilder.DropTable(
                name: "handled_requests",
                schema: "feedback");

            migrationBuilder.DropTable(
                name: "notes",
                schema: "feedback");

            migrationBuilder.DropTable(
                name: "steps",
                schema: "feedback");

            migrationBuilder.DropTable(
                name: "upload_asks",
                schema: "feedback");

            migrationBuilder.DropTable(
                name: "tickets",
                schema: "feedback");
        }
    }
}
