using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Ministries.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class LifeGroupsTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "shows_in_life_groups",
                schema: "ministries",
                table: "ministries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE ministries.ministries
                SET shows_in_life_groups = kind = 'life-group',
                    meets = CASE WHEN room = '' THEN meets WHEN meets = '' THEN room ELSE meets || ', ' || room END;
                UPDATE ministries.ministries SET round_id = NULL WHERE NOT shows_in_life_groups;
                """);

            migrationBuilder.DropIndex(
                name: "ix_rounds_ministry_id",
                schema: "ministries",
                table: "rounds");

            migrationBuilder.DropColumn(
                name: "ministry_id",
                schema: "ministries",
                table: "rounds");

            migrationBuilder.DropColumn(
                name: "kind",
                schema: "ministries",
                table: "ministries");

            migrationBuilder.DropColumn(
                name: "room",
                schema: "ministries",
                table: "ministries");

            migrationBuilder.DropColumn(
                name: "join_as_parent",
                schema: "ministries",
                table: "ministries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ministry_id",
                schema: "ministries",
                table: "rounds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "kind",
                schema: "ministries",
                table: "ministries",
                type: "text",
                nullable: false,
                defaultValue: "ministry");

            migrationBuilder.AddColumn<string>(
                name: "room",
                schema: "ministries",
                table: "ministries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "join_as_parent",
                schema: "ministries",
                table: "ministries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE ministries.ministries SET kind = 'life-group' WHERE shows_in_life_groups;");

            migrationBuilder.DropColumn(
                name: "shows_in_life_groups",
                schema: "ministries",
                table: "ministries");

            migrationBuilder.CreateIndex(
                name: "ix_rounds_ministry_id",
                schema: "ministries",
                table: "rounds",
                column: "ministry_id");
        }
    }
}
