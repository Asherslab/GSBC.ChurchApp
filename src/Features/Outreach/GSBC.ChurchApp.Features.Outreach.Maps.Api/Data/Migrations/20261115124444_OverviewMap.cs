using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OverviewMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "overviews",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    map_key = table.Column<string>(type: "text", nullable: false),
                    map_bytes = table.Column<long>(type: "bigint", nullable: false),
                    max_zoom = table.Column<int>(type: "integer", nullable: false),
                    built_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_overviews", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "overviews",
                schema: "outreach_maps");
        }
    }
}
