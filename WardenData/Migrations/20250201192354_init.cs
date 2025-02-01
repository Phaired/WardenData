using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WardenData.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OrderEffects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effect_name = table.Column<string>(type: "text", nullable: false),
                    min_value = table.Column<long>(type: "bigint", nullable: false),
                    max_value = table.Column<long>(type: "bigint", nullable: false),
                    desired_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderEffects", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_effects_orders",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<long>(type: "bigint", nullable: false),
                    initial_effects = table.Column<string>(type: "jsonb", nullable: false),
                    runes_prices = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_sessions_orders",
                        column: x => x.order_id,
                        principalTable: "Orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuneHistories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_id = table.Column<int>(type: "integer", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rune_id = table.Column<int>(type: "integer", nullable: false),
                    is_tenta = table.Column<bool>(type: "boolean", nullable: false),
                    effects_after = table.Column<string>(type: "jsonb", nullable: false),
                    has_succeed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuneHistories", x => x.id);
                    table.ForeignKey(
                        name: "fk_rune_histories_sessions",
                        column: x => x.session_id,
                        principalTable: "Sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderEffects_order_id",
                table: "OrderEffects",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_name",
                table: "Orders",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuneHistories_session_id",
                table: "RuneHistories",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_order_id",
                table: "Sessions",
                column: "order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderEffects");

            migrationBuilder.DropTable(
                name: "RuneHistories");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
