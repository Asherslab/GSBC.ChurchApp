using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Configuration.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Drafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "draft_id",
                schema: "config",
                table: "changes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "draft_name",
                schema: "config",
                table: "changes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "drafts",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    asked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    layer_on = table.Column<bool>(type: "boolean", nullable: false),
                    layer_starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    layer_ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    dropped_from_layer = table.Column<string>(type: "jsonb", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_drafts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "draft_changes",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    draft_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<string>(type: "text", nullable: true),
                    placement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    surface = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    based_on_revision = table.Column<long>(type: "bigint", nullable: false),
                    patch = table.Column<string>(type: "jsonb", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_draft_changes", x => x.id);
                    table.ForeignKey(
                        name: "fk_draft_changes_drafts_draft_id",
                        column: x => x.draft_id,
                        principalSchema: "config",
                        principalTable: "drafts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_changes_draft_id",
                schema: "config",
                table: "changes",
                column: "draft_id");

            migrationBuilder.CreateIndex(
                name: "ix_draft_changes_draft_id_placement_id",
                schema: "config",
                table: "draft_changes",
                columns: new[] { "draft_id", "placement_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_draft_changes_placement_id",
                schema: "config",
                table: "draft_changes",
                column: "placement_id");

            migrationBuilder.CreateIndex(
                name: "ix_drafts_layer_on",
                schema: "config",
                table: "drafts",
                column: "layer_on",
                unique: true,
                filter: "layer_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "draft_changes",
                schema: "config");

            migrationBuilder.DropTable(
                name: "drafts",
                schema: "config");

            migrationBuilder.DropIndex(
                name: "ix_changes_draft_id",
                schema: "config",
                table: "changes");

            migrationBuilder.DropColumn(
                name: "draft_id",
                schema: "config",
                table: "changes");

            migrationBuilder.DropColumn(
                name: "draft_name",
                schema: "config",
                table: "changes");
        }
    }
}
