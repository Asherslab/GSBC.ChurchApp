using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SharedSquares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "squares",
                schema: "outreach_maps",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    row = table.Column<int>(type: "integer", nullable: false),
                    column = table.Column<int>(type: "integer", nullable: false),
                    pending = table.Column<bool>(type: "boolean", nullable: false),
                    empty = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_squares", x => x.id);
                });

            migrationBuilder.Sql("""
                INSERT INTO outreach_maps.squares (id, "row", "column", pending, empty, version, name)
                SELECT id, "row", "column", pending AND "on", empty AND "on", version, name FROM outreach_maps.cells;
                """);

            migrationBuilder.AddColumn<string>(
                name: "square_id",
                schema: "outreach_maps",
                table: "cells",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE outreach_maps.cells SET square_id = id;");

            migrationBuilder.DropPrimaryKey(
                name: "pk_cells",
                schema: "outreach_maps",
                table: "cells");

            migrationBuilder.DropColumn(name: "id", schema: "outreach_maps", table: "cells");
            migrationBuilder.DropColumn(name: "empty", schema: "outreach_maps", table: "cells");
            migrationBuilder.DropColumn(name: "pending", schema: "outreach_maps", table: "cells");
            migrationBuilder.DropColumn(name: "version", schema: "outreach_maps", table: "cells");
            migrationBuilder.DropColumn(name: "name", schema: "outreach_maps", table: "cells");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cells",
                schema: "outreach_maps",
                table: "cells",
                columns: new[] { "area_id", "square_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cells_square_id",
                schema: "outreach_maps",
                table: "cells",
                column: "square_id");

            migrationBuilder.RenameColumn(
                name: "area_id",
                schema: "outreach_maps",
                table: "regions",
                newName: "grid_id");

            migrationBuilder.RenameIndex(
                name: "ix_regions_area_id",
                schema: "outreach_maps",
                table: "regions",
                newName: "ix_regions_grid_id");

            migrationBuilder.AddColumn<string>(
                name: "grid_id",
                schema: "outreach_maps",
                table: "areas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE outreach_maps.areas SET grid_id = id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "grid_id", schema: "outreach_maps", table: "areas");

            migrationBuilder.RenameIndex(
                name: "ix_regions_grid_id",
                schema: "outreach_maps",
                table: "regions",
                newName: "ix_regions_area_id");

            migrationBuilder.RenameColumn(
                name: "grid_id",
                schema: "outreach_maps",
                table: "regions",
                newName: "area_id");

            migrationBuilder.DropIndex(name: "ix_cells_square_id", schema: "outreach_maps", table: "cells");
            migrationBuilder.DropPrimaryKey(name: "pk_cells", schema: "outreach_maps", table: "cells");

            migrationBuilder.AddColumn<string>(name: "id", schema: "outreach_maps", table: "cells", type: "text", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(name: "empty", schema: "outreach_maps", table: "cells", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "pending", schema: "outreach_maps", table: "cells", type: "boolean", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<long>(name: "version", schema: "outreach_maps", table: "cells", type: "bigint", nullable: false, defaultValue: 0L);
            migrationBuilder.AddColumn<string>(name: "name", schema: "outreach_maps", table: "cells", type: "text", nullable: false, defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE outreach_maps.cells c SET id = c.area_id || '/' || c.square_id, pending = s.pending, empty = s.empty, version = s.version, name = s.name
                FROM outreach_maps.squares s WHERE s.id = c.square_id;
                """);

            migrationBuilder.DropColumn(name: "square_id", schema: "outreach_maps", table: "cells");

            migrationBuilder.AddPrimaryKey(
                name: "pk_cells",
                schema: "outreach_maps",
                table: "cells",
                column: "id");

            migrationBuilder.DropTable(
                name: "squares",
                schema: "outreach_maps");
        }
    }
}
