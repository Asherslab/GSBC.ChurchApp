using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OutreachMaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "outreach_maps");

            migrationBuilder.CreateTable(
                name: "areas",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    south = table.Column<double>(type: "double precision", nullable: false),
                    west = table.Column<double>(type: "double precision", nullable: false),
                    north = table.Column<double>(type: "double precision", nullable: false),
                    east = table.Column<double>(type: "double precision", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    stage = table.Column<string>(type: "text", nullable: true),
                    problem = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    built_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    report = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_areas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "files",
                schema: "outreach_maps",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    region_id = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files", x => new { x.key, x.region_id, x.version });
                });

            migrationBuilder.CreateTable(
                name: "regions",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    area_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    core_south = table.Column<double>(type: "double precision", nullable: false),
                    core_west = table.Column<double>(type: "double precision", nullable: false),
                    core_north = table.Column<double>(type: "double precision", nullable: false),
                    core_east = table.Column<double>(type: "double precision", nullable: false),
                    south = table.Column<double>(type: "double precision", nullable: false),
                    west = table.Column<double>(type: "double precision", nullable: false),
                    north = table.Column<double>(type: "double precision", nullable: false),
                    east = table.Column<double>(type: "double precision", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    built_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    streets_key = table.Column<string>(type: "text", nullable: false),
                    streets_fingerprint = table.Column<string>(type: "text", nullable: false),
                    streets_bytes = table.Column<long>(type: "bigint", nullable: false),
                    map_key = table.Column<string>(type: "text", nullable: false),
                    map_bytes = table.Column<long>(type: "bigint", nullable: false),
                    map_max_zoom = table.Column<int>(type: "integer", nullable: false),
                    letterboxes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_regions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_files_region_id",
                schema: "outreach_maps",
                table: "files",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_regions_area_id",
                schema: "outreach_maps",
                table: "regions",
                column: "area_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "areas",
                schema: "outreach_maps");

            migrationBuilder.DropTable(
                name: "files",
                schema: "outreach_maps");

            migrationBuilder.DropTable(
                name: "regions",
                schema: "outreach_maps");
        }
    }
}
