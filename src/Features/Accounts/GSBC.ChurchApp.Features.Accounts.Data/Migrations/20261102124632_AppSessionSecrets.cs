using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Accounts.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppSessionSecrets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "secret_hash",
                schema: "accounts",
                table: "sessions",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sessions_secret_hash",
                schema: "accounts",
                table: "sessions",
                column: "secret_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sessions_secret_hash",
                schema: "accounts",
                table: "sessions");

            migrationBuilder.DropColumn(
                name: "secret_hash",
                schema: "accounts",
                table: "sessions");
        }
    }
}
