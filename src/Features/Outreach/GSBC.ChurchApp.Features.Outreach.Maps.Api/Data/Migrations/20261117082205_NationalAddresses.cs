using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NationalAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "national_addresses",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    release = table.Column<int>(type: "integer", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    locality = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    postcode = table.Column<string>(type: "text", nullable: false),
                    building = table.Column<string>(type: "text", nullable: false),
                    building_key = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_national_addresses", x => new { x.id, x.release });
                });

            migrationBuilder.CreateTable(
                name: "national_localities",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    release = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_national_localities", x => new { x.id, x.release });
                });

            migrationBuilder.CreateTable(
                name: "national_releases",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    addresses = table.Column<long>(type: "bigint", nullable: false),
                    localities = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_national_releases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "place_maps",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    map_key = table.Column<string>(type: "text", nullable: false),
                    map_bytes = table.Column<long>(type: "bigint", nullable: false),
                    max_zoom = table.Column<int>(type: "integer", nullable: false),
                    south = table.Column<double>(type: "double precision", nullable: false),
                    west = table.Column<double>(type: "double precision", nullable: false),
                    north = table.Column<double>(type: "double precision", nullable: false),
                    east = table.Column<double>(type: "double precision", nullable: false),
                    built_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_place_maps", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_national_addresses_building_key",
                schema: "outreach_maps",
                table: "national_addresses",
                column: "building_key",
                filter: "building_key <> ''")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_national_addresses_key",
                schema: "outreach_maps",
                table: "national_addresses",
                column: "key")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_national_addresses_latitude_longitude",
                schema: "outreach_maps",
                table: "national_addresses",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "ix_national_addresses_number_key",
                schema: "outreach_maps",
                table: "national_addresses",
                columns: new[] { "number", "key" })
                .Annotation("Npgsql:IndexOperators", new[] { "text_ops", "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_national_addresses_release",
                schema: "outreach_maps",
                table: "national_addresses",
                column: "release");

            migrationBuilder.CreateIndex(
                name: "ix_national_localities_key",
                schema: "outreach_maps",
                table: "national_localities",
                column: "key")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_place_maps_map_key",
                schema: "outreach_maps",
                table: "place_maps",
                column: "map_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "national_addresses",
                schema: "outreach_maps");

            migrationBuilder.DropTable(
                name: "national_localities",
                schema: "outreach_maps");

            migrationBuilder.DropTable(
                name: "national_releases",
                schema: "outreach_maps");

            migrationBuilder.DropTable(
                name: "place_maps",
                schema: "outreach_maps");
        }
    }
}
