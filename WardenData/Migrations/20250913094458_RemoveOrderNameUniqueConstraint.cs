using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WardenData.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderNameUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_orders_name",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_orders_name",
                table: "Orders",
                column: "name",
                unique: true);
        }
    }
}
