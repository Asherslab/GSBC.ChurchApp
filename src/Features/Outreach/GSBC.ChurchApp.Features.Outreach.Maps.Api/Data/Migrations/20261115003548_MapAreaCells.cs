using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MapAreaCells : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "cell_latitude",
                schema: "outreach_maps",
                table: "areas",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "cell_longitude",
                schema: "outreach_maps",
                table: "areas",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "centre_latitude",
                schema: "outreach_maps",
                table: "areas",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "centre_longitude",
                schema: "outreach_maps",
                table: "areas",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                schema: "outreach_maps",
                table: "areas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "origin_column",
                schema: "outreach_maps",
                table: "areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "origin_row",
                schema: "outreach_maps",
                table: "areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "steps",
                schema: "outreach_maps",
                table: "areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "steps_done",
                schema: "outreach_maps",
                table: "areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "cells",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    area_id = table.Column<string>(type: "text", nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    column = table.Column<int>(type: "integer", nullable: false),
                    on = table.Column<bool>(type: "boolean", nullable: false),
                    pending = table.Column<bool>(type: "boolean", nullable: false),
                    empty = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cells", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "places",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    population = table.Column<int>(type: "integer", nullable: false),
                    indexed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_places", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cells_area_id_row_column",
                schema: "outreach_maps",
                table: "cells",
                columns: new[] { "area_id", "row", "column" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cells",
                schema: "outreach_maps");

            migrationBuilder.DropTable(
                name: "places",
                schema: "outreach_maps");

            migrationBuilder.DropColumn(
                name: "cell_latitude",
                schema: "outreach_maps",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "cell_longitude",
                schema: "outreach_maps",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "centre_latitude",
                schema: "outreach_maps",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "centre_longitude",
                schema: "outreach_maps",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "outreach_maps",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "origin_column",
                schema: "outreach_maps",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "origin_row",
                schema: "outreach_maps",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "steps",
                schema: "outreach_maps",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "steps_done",
                schema: "outreach_maps",
                table: "areas");
        }
    }
}
