using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Media.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaUsesAndDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "height",
                schema: "media",
                table: "pending_uploads");

            migrationBuilder.DropColumn(
                name: "width",
                schema: "media",
                table: "pending_uploads");

            migrationBuilder.CreateTable(
                name: "uses",
                schema: "media",
                columns: table => new
                {
                    media_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_uses", x => new { x.media_id, x.owner });
                    table.ForeignKey(
                        name: "fk_uses_items_media_id",
                        column: x => x.media_id,
                        principalSchema: "media",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_uses_owner",
                schema: "media",
                table: "uses",
                column: "owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "uses",
                schema: "media");

            migrationBuilder.AddColumn<int>(
                name: "height",
                schema: "media",
                table: "pending_uploads",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "width",
                schema: "media",
                table: "pending_uploads",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
