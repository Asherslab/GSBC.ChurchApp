using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Configuration.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "config");

            migrationBuilder.CreateTable(
                name: "changes",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<string>(type: "text", nullable: true),
                    placement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    surface = table.Column<string>(type: "text", nullable: false),
                    before = table.Column<string>(type: "jsonb", nullable: true),
                    after = table.Column<string>(type: "jsonb", nullable: true),
                    at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    by = table.Column<Guid>(type: "uuid", nullable: false),
                    rolls_back = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_changes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "placements",
                schema: "config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "text", nullable: false),
                    scope_id = table.Column<string>(type: "text", nullable: true),
                    surface = table.Column<string>(type: "text", nullable: false),
                    is_singleton = table.Column<bool>(type: "boolean", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    hidden = table.Column<bool>(type: "boolean", nullable: false),
                    audience = table.Column<string>(type: "text", nullable: false),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    values = table.Column<string>(type: "jsonb", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_placements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_changes_area_scope_id_at",
                schema: "config",
                table: "changes",
                columns: new[] { "area", "scope_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_changes_batch_id",
                schema: "config",
                table: "changes",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_placements_area_scope_id",
                schema: "config",
                table: "placements",
                columns: new[] { "area", "scope_id" });

            migrationBuilder.CreateIndex(
                name: "ix_placements_area_scope_id_surface",
                schema: "config",
                table: "placements",
                columns: new[] { "area", "scope_id", "surface" },
                unique: true,
                filter: "is_singleton")
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "changes",
                schema: "config");

            migrationBuilder.DropTable(
                name: "placements",
                schema: "config");
        }
    }
}
