using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WardenData.Migrations
{
    /// <inheritdoc />
    public partial class NormalizedSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RuneHistories_session_id",
                table: "RuneHistories");

            migrationBuilder.AlterColumn<string>(
                name: "runes_prices",
                table: "Sessions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "initial_effects",
                table: "Sessions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "effects_after",
                table: "RuneHistories",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AddColumn<DateTime>(
                name: "applied_at",
                table: "RuneHistories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "effect_name",
                table: "OrderEffects",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<short>(
                name: "effect_id",
                table: "OrderEffects",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "Effects",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: true),
                    is_percent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    min_possible = table.Column<int>(type: "integer", nullable: true),
                    max_possible = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Effects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Runes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "RuneHistoryEffectChanges",
                columns: table => new
                {
                    rune_history_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effect_id = table.Column<short>(type: "smallint", nullable: false),
                    old_value = table.Column<int>(type: "integer", nullable: true),
                    new_value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuneHistoryEffectChanges", x => new { x.rune_history_id, x.effect_id });
                    table.ForeignKey(
                        name: "fk_rune_history_effect_changes_effects",
                        column: x => x.effect_id,
                        principalTable: "Effects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rune_history_effect_changes_rune_histories",
                        column: x => x.rune_history_id,
                        principalTable: "RuneHistories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionInitialEffects",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effect_id = table.Column<short>(type: "smallint", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionInitialEffects", x => new { x.session_id, x.effect_id });
                    table.ForeignKey(
                        name: "fk_session_initial_effects_effects",
                        column: x => x.effect_id,
                        principalTable: "Effects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_session_initial_effects_sessions",
                        column: x => x.session_id,
                        principalTable: "Sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionRunePrices",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rune_id = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRunePrices", x => new { x.session_id, x.rune_id });
                    table.ForeignKey(
                        name: "fk_session_rune_prices_runes",
                        column: x => x.rune_id,
                        principalTable: "Runes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_session_rune_prices_sessions",
                        column: x => x.session_id,
                        principalTable: "Sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sessions_started_at",
                table: "Sessions",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_rune_histories_applied_at",
                table: "RuneHistories",
                column: "applied_at");

            migrationBuilder.CreateIndex(
                name: "IX_RuneHistories_rune_id",
                table: "RuneHistories",
                column: "rune_id");

            migrationBuilder.CreateIndex(
                name: "uq_rh_session_step",
                table: "RuneHistories",
                columns: new[] { "session_id", "original_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderEffects_effect_id",
                table: "OrderEffects",
                column: "effect_id");

            migrationBuilder.CreateIndex(
                name: "ix_effects_code",
                table: "Effects",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_effects_name",
                table: "Effects",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rhec_effect",
                table: "RuneHistoryEffectChanges",
                columns: new[] { "effect_id", "rune_history_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sie_effect",
                table: "SessionInitialEffects",
                column: "effect_id");

            migrationBuilder.CreateIndex(
                name: "ix_srp_rune",
                table: "SessionRunePrices",
                column: "rune_id");

            migrationBuilder.AddForeignKey(
                name: "fk_order_effects_effects",
                table: "OrderEffects",
                column: "effect_id",
                principalTable: "Effects",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_rune_histories_runes",
                table: "RuneHistories",
                column: "rune_id",
                principalTable: "Runes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_effects_effects",
                table: "OrderEffects");

            migrationBuilder.DropForeignKey(
                name: "fk_rune_histories_runes",
                table: "RuneHistories");

            migrationBuilder.DropTable(
                name: "RuneHistoryEffectChanges");

            migrationBuilder.DropTable(
                name: "SessionInitialEffects");

            migrationBuilder.DropTable(
                name: "SessionRunePrices");

            migrationBuilder.DropTable(
                name: "Effects");

            migrationBuilder.DropTable(
                name: "Runes");

            migrationBuilder.DropIndex(
                name: "ix_sessions_started_at",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "ix_rune_histories_applied_at",
                table: "RuneHistories");

            migrationBuilder.DropIndex(
                name: "IX_RuneHistories_rune_id",
                table: "RuneHistories");

            migrationBuilder.DropIndex(
                name: "uq_rh_session_step",
                table: "RuneHistories");

            migrationBuilder.DropIndex(
                name: "IX_OrderEffects_effect_id",
                table: "OrderEffects");

            migrationBuilder.DropColumn(
                name: "started_at",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "applied_at",
                table: "RuneHistories");

            migrationBuilder.DropColumn(
                name: "effect_id",
                table: "OrderEffects");

            migrationBuilder.AlterColumn<string>(
                name: "runes_prices",
                table: "Sessions",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "initial_effects",
                table: "Sessions",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "effects_after",
                table: "RuneHistories",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "effect_name",
                table: "OrderEffects",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuneHistories_session_id",
                table: "RuneHistories",
                column: "session_id");
        }
    }
}
