using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Chats.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Chats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chats");

            migrationBuilder.CreateTable(
                name: "audit",
                schema: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    words = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "chats",
                schema: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ministry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    audience = table.Column<string>(type: "text", nullable: false),
                    asker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_seq = table.Column<long>(type: "bigint", nullable: false),
                    flags = table.Column<List<string>>(type: "text[]", nullable: false),
                    has_minors = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consents",
                schema: "chats",
                columns: table => new
                {
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved = table.Column<bool>(type: "boolean", nullable: true),
                    decided_by = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consents", x => new { x.chat_id, x.child_id });
                });

            migrationBuilder.CreateTable(
                name: "message_versions",
                schema: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    attachments = table.Column<string>(type: "jsonb", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    by_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_versions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seq = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: true),
                    for_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<string>(type: "text", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    reply_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mentions = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    attachments = table.Column<string>(type: "jsonb", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "participants",
                schema: "chats",
                columns: table => new
                {
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    for_children = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    since = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_seq = table.Column<long>(type: "bigint", nullable: false),
                    muted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participants", x => new { x.chat_id, x.person_id });
                });

            migrationBuilder.CreateTable(
                name: "reactions",
                schema: "chats",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    emoji = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reactions", x => new { x.message_id, x.person_id, x.emoji });
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "chats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to = table.Column<string>(type: "text", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    handled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    outcome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_action_at",
                schema: "chats",
                table: "audit",
                columns: new[] { "action", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_chat_id_at",
                schema: "chats",
                table: "audit",
                columns: new[] { "chat_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_chats_ministry_id",
                schema: "chats",
                table: "chats",
                column: "ministry_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_versions_message_id_number",
                schema: "chats",
                table: "message_versions",
                columns: new[] { "message_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_chat_id_seq",
                schema: "chats",
                table: "messages",
                columns: new[] { "chat_id", "seq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_messages_sender_id_client_id",
                schema: "chats",
                table: "messages",
                columns: new[] { "sender_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_participants_person_id",
                schema: "chats",
                table: "participants",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_chat_id",
                schema: "chats",
                table: "reports",
                column: "chat_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit",
                schema: "chats");

            migrationBuilder.DropTable(
                name: "chats",
                schema: "chats");

            migrationBuilder.DropTable(
                name: "consents",
                schema: "chats");

            migrationBuilder.DropTable(
                name: "message_versions",
                schema: "chats");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "chats");

            migrationBuilder.DropTable(
                name: "participants",
                schema: "chats");

            migrationBuilder.DropTable(
                name: "reactions",
                schema: "chats");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "chats");
        }
    }
}
