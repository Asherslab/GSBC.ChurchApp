using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GSBC.ChurchApp.Features.Notifications.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NotificationDigests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "digest_schedules",
                schema: "notifications",
                columns: table => new
                {
                    kind = table.Column<string>(type: "text", nullable: false),
                    after_minutes = table.Column<int[]>(type: "integer[]", nullable: false),
                    daily_at = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digest_schedules", x => x.kind);
                });

            migrationBuilder.CreateTable(
                name: "digests",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    group_key = table.Column<string>(type: "text", nullable: false),
                    audience_kind = table.Column<string>(type: "text", nullable: false),
                    audience_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audience_node = table.Column<string>(type: "text", nullable: true),
                    audience_object_id = table.Column<string>(type: "text", nullable: true),
                    link = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<string>(type: "text", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_through = table.Column<long>(type: "bigint", nullable: false),
                    last_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "digest_items",
                schema: "notifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    digest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_digest_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_digest_items_digests_digest_id",
                        column: x => x.digest_id,
                        principalSchema: "notifications",
                        principalTable: "digests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_digest_items_digest_id_id",
                schema: "notifications",
                table: "digest_items",
                columns: new[] { "digest_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_digests_due_at",
                schema: "notifications",
                table: "digests",
                column: "due_at");

            migrationBuilder.CreateIndex(
                name: "ix_digests_kind_group_key",
                schema: "notifications",
                table: "digests",
                columns: new[] { "kind", "group_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "digest_items",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "digest_schedules",
                schema: "notifications");

            migrationBuilder.DropTable(
                name: "digests",
                schema: "notifications");
        }
    }
}
