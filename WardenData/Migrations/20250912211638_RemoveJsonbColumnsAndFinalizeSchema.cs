using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WardenData.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJsonbColumnsAndFinalizeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialEffects",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RunesPrices",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "EffectsAfter",
                table: "RuneHistories");

            migrationBuilder.DropColumn(
                name: "EffectName",
                table: "OrderEffects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InitialEffects",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RunesPrices",
                table: "Sessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EffectsAfter",
                table: "RuneHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EffectName",
                table: "OrderEffects",
                type: "text",
                nullable: true);
        }
    }
}
