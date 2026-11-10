using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MinistryParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "calendars_from_below",
                schema: "ministries",
                table: "ministries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "parts_off",
                schema: "ministries",
                table: "ministries",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "calendars_from_below",
                schema: "ministries",
                table: "ministries");

            migrationBuilder.DropColumn(
                name: "parts_off",
                schema: "ministries",
                table: "ministries");
        }
    }
}
