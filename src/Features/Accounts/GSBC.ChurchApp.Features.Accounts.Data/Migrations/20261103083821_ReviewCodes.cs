using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GSBC.ChurchApp.Features.Accounts.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReviewCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "review_code_id",
                schema: "accounts",
                table: "sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "review_codes",
                schema: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    turned_off_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    uses = table.Column<int>(type: "integer", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_review_codes", x => x.id);
                    table.ForeignKey(
                        name: "fk_review_codes_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "accounts",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_review_code_id",
                schema: "accounts",
                table: "sessions",
                column: "review_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_action_at",
                schema: "accounts",
                table: "audit",
                columns: new[] { "action", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_review_codes_hash",
                schema: "accounts",
                table: "review_codes",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_review_codes_user_id",
                schema: "accounts",
                table: "review_codes",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_sessions_review_codes_review_code_id",
                schema: "accounts",
                table: "sessions",
                column: "review_code_id",
                principalSchema: "accounts",
                principalTable: "review_codes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sessions_review_codes_review_code_id",
                schema: "accounts",
                table: "sessions");

            migrationBuilder.DropTable(
                name: "review_codes",
                schema: "accounts");

            migrationBuilder.DropIndex(
                name: "ix_sessions_review_code_id",
                schema: "accounts",
                table: "sessions");

            migrationBuilder.DropIndex(
                name: "ix_audit_action_at",
                schema: "accounts",
                table: "audit");

            migrationBuilder.DropColumn(
                name: "review_code_id",
                schema: "accounts",
                table: "sessions");
        }
    }
}
