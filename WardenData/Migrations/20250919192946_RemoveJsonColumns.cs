using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WardenData.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJsonColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "initial_effects",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "runes_prices",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "effects_after",
                table: "RuneHistories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "initial_effects",
                table: "Sessions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "runes_prices",
                table: "Sessions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "effects_after",
                table: "RuneHistories",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}
