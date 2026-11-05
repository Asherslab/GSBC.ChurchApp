using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Media.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaContentHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sha256",
                schema: "media",
                table: "items",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("UPDATE media.items SET sha256 = split_part(key, '.', 1);");

            migrationBuilder.AlterColumn<string>(
                name: "sha256",
                schema: "media",
                table: "items",
                type: "character(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(64)",
                oldFixedLength: true,
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_items_sha256",
                schema: "media",
                table: "items",
                column: "sha256");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_items_sha256",
                schema: "media",
                table: "items");

            migrationBuilder.DropColumn(
                name: "sha256",
                schema: "media",
                table: "items");
        }
    }
}
