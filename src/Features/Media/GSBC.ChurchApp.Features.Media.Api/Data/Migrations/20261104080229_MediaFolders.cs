using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Media.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaFolders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "folder_id",
                schema: "media",
                table: "pending_uploads",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "folder_id",
                schema: "media",
                table: "items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "folders",
                schema: "media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    system_area = table.Column<string>(type: "text", nullable: true),
                    system_scope = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_folders", x => x.id);
                    table.ForeignKey(
                        name: "fk_folders_folders_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "media",
                        principalTable: "folders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_items_folder_id",
                schema: "media",
                table: "items",
                column: "folder_id");

            migrationBuilder.CreateIndex(
                name: "ix_folders_parent_id",
                schema: "media",
                table: "folders",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_folders_system_area_system_scope",
                schema: "media",
                table: "folders",
                columns: new[] { "system_area", "system_scope" },
                unique: true,
                filter: "system_area IS NOT NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddForeignKey(
                name: "fk_items_folders_folder_id",
                schema: "media",
                table: "items",
                column: "folder_id",
                principalSchema: "media",
                principalTable: "folders",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_items_folders_folder_id",
                schema: "media",
                table: "items");

            migrationBuilder.DropTable(
                name: "folders",
                schema: "media");

            migrationBuilder.DropIndex(
                name: "ix_items_folder_id",
                schema: "media",
                table: "items");

            migrationBuilder.DropColumn(
                name: "folder_id",
                schema: "media",
                table: "pending_uploads");

            migrationBuilder.DropColumn(
                name: "folder_id",
                schema: "media",
                table: "items");
        }
    }
}
