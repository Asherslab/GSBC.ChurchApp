using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PartsOrderAndChatStarters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chat_starters",
                schema: "ministries",
                table: "ministries",
                type: "text",
                nullable: false,
                defaultValue: "leaders");

            migrationBuilder.AddColumn<List<string>>(
                name: "parts_order",
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
                name: "chat_starters",
                schema: "ministries",
                table: "ministries");

            migrationBuilder.DropColumn(
                name: "parts_order",
                schema: "ministries",
                table: "ministries");
        }
    }
}
