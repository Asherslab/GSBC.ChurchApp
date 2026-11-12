using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Rosters.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepeatingDaysAway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "to",
                schema: "rosters",
                table: "away",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "end_time",
                schema: "rosters",
                table: "away",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<List<DateOnly>>(
                name: "except",
                schema: "rosters",
                table: "away",
                type: "date[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "start_time",
                schema: "rosters",
                table: "away",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<List<int>>(
                name: "weekdays",
                schema: "rosters",
                table: "away",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<bool>(
                name: "weekly",
                schema: "rosters",
                table: "away",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_time",
                schema: "rosters",
                table: "away");

            migrationBuilder.DropColumn(
                name: "except",
                schema: "rosters",
                table: "away");

            migrationBuilder.DropColumn(
                name: "start_time",
                schema: "rosters",
                table: "away");

            migrationBuilder.DropColumn(
                name: "weekdays",
                schema: "rosters",
                table: "away");

            migrationBuilder.DropColumn(
                name: "weekly",
                schema: "rosters",
                table: "away");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "to",
                schema: "rosters",
                table: "away",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);
        }
    }
}
