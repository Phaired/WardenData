using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WardenData.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeJsonData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuneHistoryEffects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rune_history_id = table.Column<int>(type: "integer", nullable: false),
                    effect_name = table.Column<string>(type: "text", nullable: false),
                    current_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuneHistoryEffects", x => x.Id);
                    table.ForeignKey(
                        name: "fk_rune_history_effects_rune_histories",
                        column: x => x.rune_history_id,
                        principalTable: "RuneHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionEffects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    effect_name = table.Column<string>(type: "text", nullable: false),
                    current_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionEffects", x => x.Id);
                    table.ForeignKey(
                        name: "fk_session_effects_sessions",
                        column: x => x.session_id,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRunePrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    session_id = table.Column<int>(type: "integer", nullable: false),
                    rune_id = table.Column<int>(type: "integer", nullable: false),
                    rune_name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRunePrices", x => x.Id);
                    table.ForeignKey(
                        name: "fk_session_rune_prices_sessions",
                        column: x => x.session_id,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuneHistoryEffects_rune_history_id",
                table: "RuneHistoryEffects",
                column: "rune_history_id");

            migrationBuilder.CreateIndex(
                name: "IX_SessionEffects_session_id",
                table: "SessionEffects",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRunePrices_session_id",
                table: "SessionRunePrices",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuneHistoryEffects");

            migrationBuilder.DropTable(
                name: "SessionEffects");

            migrationBuilder.DropTable(
                name: "SessionRunePrices");
        }
    }
}
