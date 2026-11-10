using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PartsFromBelow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "parts_from_below",
                schema: "ministries",
                table: "ministries",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.Sql("UPDATE ministries.ministries SET parts_from_below = '{calendar}' WHERE calendars_from_below");

            migrationBuilder.DropColumn(
                name: "calendars_from_below",
                schema: "ministries",
                table: "ministries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "calendars_from_below",
                schema: "ministries",
                table: "ministries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE ministries.ministries SET calendars_from_below = 'calendar' = ANY(parts_from_below)");

            migrationBuilder.DropColumn(
                name: "parts_from_below",
                schema: "ministries",
                table: "ministries");
        }
    }
}
