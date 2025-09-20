using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WardenData.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_name",
                table: "Orders");

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "Sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "RuneHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_user_id",
                table: "Sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_RuneHistories_user_id",
                table: "RuneHistories",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_user_name",
                table: "Orders",
                columns: new[] { "user_id", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_users_token",
                table: "Users",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "Users",
                column: "username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_orders_users",
                table: "Orders",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_rune_histories_users",
                table: "RuneHistories",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_sessions_users",
                table: "Sessions",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orders_users",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "fk_rune_histories_users",
                table: "RuneHistories");

            migrationBuilder.DropForeignKey(
                name: "fk_sessions_users",
                table: "Sessions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "ix_sessions_user_id",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_RuneHistories_user_id",
                table: "RuneHistories");

            migrationBuilder.DropIndex(
                name: "ix_orders_user_name",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "RuneHistories");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "Orders");

            migrationBuilder.CreateIndex(
                name: "ix_orders_name",
                table: "Orders",
                column: "name",
                unique: true);
        }
    }
}
