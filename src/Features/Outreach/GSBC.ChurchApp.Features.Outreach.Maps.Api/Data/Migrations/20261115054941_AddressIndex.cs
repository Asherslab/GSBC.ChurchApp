using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddressIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "addresses",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    region_id = table.Column<string>(type: "text", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    locality = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => new { x.id, x.region_id });
                });

            migrationBuilder.CreateIndex(
                name: "ix_addresses_key",
                schema: "outreach_maps",
                table: "addresses",
                column: "key")
                .Annotation("Npgsql:IndexOperators", new[] { "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_addresses_number_key",
                schema: "outreach_maps",
                table: "addresses",
                columns: new[] { "number", "key" })
                .Annotation("Npgsql:IndexOperators", new[] { "text_ops", "text_pattern_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_addresses_region_id",
                schema: "outreach_maps",
                table: "addresses",
                column: "region_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses",
                schema: "outreach_maps");
        }
    }
}
