using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Prayer.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ForgetPrayedToldAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "prayed_told_at",
                schema: "prayer",
                table: "requests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "prayed_told_at",
                schema: "prayer",
                table: "requests",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
