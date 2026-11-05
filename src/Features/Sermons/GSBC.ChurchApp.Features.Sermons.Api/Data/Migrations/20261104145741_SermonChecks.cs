using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Sermons.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SermonChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "checked_at",
                schema: "sermons",
                table: "sermons",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "checked_by",
                schema: "sermons",
                table: "sermons",
                type: "uuid",
                nullable: true);
        
            migrationBuilder.Sql("UPDATE sermons.sermons SET checked_at = added_at WHERE checked_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "checked_at",
                schema: "sermons",
                table: "sermons");

            migrationBuilder.DropColumn(
                name: "checked_by",
                schema: "sermons",
                table: "sermons");
        }
    }
}
