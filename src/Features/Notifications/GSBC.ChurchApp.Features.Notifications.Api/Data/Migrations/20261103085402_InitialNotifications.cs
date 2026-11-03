using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GSBC.ChurchApp.Features.Notifications.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "announcements",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    link = table.Column<string>(type: "text", nullable: true),
                    audience_kind = table.Column<string>(type: "text", nullable: false),
                    audience_node = table.Column<string>(type: "text", nullable: true),
                    audience_object_id = table.Column<string>(type: "text", nullable: true),
                    audience_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    urgent = table.Column<bool>(type: "boolean", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    send_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reached_people = table.Column<int>(type: "integer", nullable: false),
                    reached_phones = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    install_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    platform = table.Column<string>(type: "text", nullable: false),
                    app_id = table.Column<string>(type: "text", nullable: false),
                    environment = table.Column<string>(type: "text", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_nudged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    area = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    link = table.Column<string>(type: "text", nullable: true),
                    subject = table.Column<string>(type: "text", nullable: true),
                    urgent = table.Column<bool>(type: "boolean", nullable: false),
                    scope_id = table.Column<string>(type: "text", nullable: true),
                    test = table.Column<bool>(type: "boolean", nullable: false),
                    raised_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "preferences",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    install_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<string>(type: "text", nullable: true),
                    push = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deliveries",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    pacing_group = table.Column<string>(type: "text", nullable: true),
                    summarised = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    lease_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gave_up_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_deliveries_devices_device_id",
                        column: x => x.device_id,
                        principalSchema: "notifications",
                        principalTable: "devices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_deliveries_notifications_notification_id",
                        column: x => x.notification_id,
                        principalSchema: "notifications",
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inbox",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox", x => x.id);
                    table.ForeignKey(
                        name: "fk_inbox_notifications_notification_id",
                        column: x => x.notification_id,
                        principalSchema: "notifications",
                        principalTable: "notifications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_announcements_state_send_at",
                schema: "notifications",
                table: "announcements",
                columns: new[] { "state", "send_at" });

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_device_id_pacing_group_created_at",
                schema: "notifications",
                table: "deliveries",
                columns: new[] { "device_id", "pacing_group", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_notification_id",
                schema: "notifications",
                table: "deliveries",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "ix_deliveries_sent_at_gave_up_at_due_at",
                schema: "notifications",
                table: "deliveries",
                columns: new[] { "sent_at", "gave_up_at", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_devices_install_id",
                schema: "notifications",
                table: "devices",
                column: "install_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_devices_user_id",
                schema: "notifications",
                table: "devices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbox_notification_id",
                schema: "notifications",
                table: "inbox",
                column: "notification_id");

            migrationBuilder.CreateIndex(
                name: "ix_inbox_user_id_read_at",
                schema: "notifications",
                table: "inbox",
                columns: new[] { "user_id", "read_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notifications_raised_at",
                schema: "notifications",
                table: "notifications",
                column: "raised_at");

            migrationBuilder.CreateIndex(
                name: "ix_preferences_user_id_install_id_kind_scope_id",
                schema: "notifications",
                table: "preferences",
                columns: new[] { "user_id", "install_id", "kind", "scope_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "announcements",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "deliveries",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "inbox",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "preferences",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "devices",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "notifications");
        }
    }
}
