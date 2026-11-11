using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Rosters.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SwapRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "direct_swaps",
                schema: "rosters",
                table: "settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "open_swaps",
                schema: "rosters",
                table: "settings",
                type: "text",
                nullable: false,
                defaultValue: "team");

            migrationBuilder.Sql("""
                UPDATE rosters.settings SET
                    open_swaps = CASE WHEN 'approve' = ANY(swap_ways) THEN 'approve' WHEN 'team' = ANY(swap_ways) THEN 'team' ELSE 'off' END,
                    direct_swaps = 'direct' = ANY(swap_ways)
                """);

            migrationBuilder.DropColumn(
                name: "swap_ways",
                schema: "rosters",
                table: "settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "swap_ways",
                schema: "rosters",
                table: "settings",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.Sql("""
                UPDATE rosters.settings SET swap_ways =
                    (CASE WHEN open_swaps = 'off' THEN ARRAY[]::text[] ELSE ARRAY[open_swaps] END)
                    || (CASE WHEN direct_swaps THEN ARRAY['direct'] ELSE ARRAY[]::text[] END)
                """);

            migrationBuilder.DropColumn(
                name: "direct_swaps",
                schema: "rosters",
                table: "settings");

            migrationBuilder.DropColumn(
                name: "open_swaps",
                schema: "rosters",
                table: "settings");
        }
    }
}
