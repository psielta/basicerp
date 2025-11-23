using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkProject.Migrations
{
    public partial class Estoque : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_location",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_virtual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_location", x => x.id);
                    table.ForeignKey(
                        name: "stock_location_org_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_outbox_event",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    aggregate_type = table.Column<string>(type: "text", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    topic = table.Column<string>(type: "text", nullable: true),
                    key = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    error = table.Column<string>(type: "text", nullable: true),
                    trace_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_outbox_event", x => x.id);
                    table.ForeignKey(
                        name: "stock_outbox_event_org_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_balance",
                schema: "public",
                columns: table => new
                {
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    on_hand = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    reserved = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    last_movement_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_balance", x => new { x.organization_id, x.location_id, x.variant_id });
                    table.CheckConstraint("stock_balance_non_negative", "on_hand >= 0 AND reserved >= 0 AND on_hand >= reserved");
                    table.ForeignKey(
                        name: "stock_balance_location_fk",
                        column: x => x.location_id,
                        principalSchema: "public",
                        principalTable: "stock_location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "stock_balance_org_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "stock_balance_variant_fk",
                        column: x => x.variant_id,
                        principalSchema: "public",
                        principalTable: "product_variant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_ledger",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    delta_on_hand = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    delta_reserved = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    movement_type = table.Column<short>(type: "smallint", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    source_type = table.Column<string>(type: "text", nullable: true),
                    source_id = table.Column<string>(type: "text", nullable: true),
                    source_line = table.Column<string>(type: "text", nullable: true),
                    deduplication_key = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_ledger", x => x.id);
                    table.CheckConstraint("stock_ledger_non_zero_delta", "delta_on_hand <> 0 OR delta_reserved <> 0");
                    table.ForeignKey(
                        name: "stock_ledger_location_fk",
                        column: x => x.location_id,
                        principalSchema: "public",
                        principalTable: "stock_location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "stock_ledger_org_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "stock_ledger_variant_fk",
                        column: x => x.variant_id,
                        principalSchema: "public",
                        principalTable: "product_variant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservation",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    reserved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_type = table.Column<string>(type: "text", nullable: true),
                    source_id = table.Column<string>(type: "text", nullable: true),
                    source_line = table.Column<string>(type: "text", nullable: true),
                    deduplication_key = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservation", x => x.id);
                    table.CheckConstraint("stock_reservation_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "stock_reservation_location_fk",
                        column: x => x.location_id,
                        principalSchema: "public",
                        principalTable: "stock_location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "stock_reservation_org_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "stock_reservation_variant_fk",
                        column: x => x.variant_id,
                        principalSchema: "public",
                        principalTable: "product_variant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_balance_location",
                schema: "public",
                table: "stock_balance",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_balance_variant",
                schema: "public",
                table: "stock_balance",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_balance_org_variant",
                schema: "public",
                table: "stock_balance",
                columns: new[] { "organization_id", "variant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_location_date",
                schema: "public",
                table: "stock_ledger",
                columns: new[] { "location_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ux_stock_ledger_org_dedup",
                schema: "public",
                table: "stock_ledger",
                columns: new[] { "organization_id", "deduplication_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_org_date",
                schema: "public",
                table: "stock_ledger",
                columns: new[] { "organization_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_variant_date",
                schema: "public",
                table: "stock_ledger",
                columns: new[] { "variant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ux_stock_location_org_default",
                schema: "public",
                table: "stock_location",
                column: "organization_id",
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "ux_stock_location_org_code",
                schema: "public",
                table: "stock_location",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_outbox_org",
                schema: "public",
                table: "stock_outbox_event",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_outbox_status_created",
                schema: "public",
                table: "stock_outbox_event",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservation_location_id",
                schema: "public",
                table: "stock_reservation",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ux_stock_reservation_org_dedup",
                schema: "public",
                table: "stock_reservation",
                columns: new[] { "organization_id", "deduplication_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservation_source",
                schema: "public",
                table: "stock_reservation",
                columns: new[] { "source_type", "source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservation_variant_status",
                schema: "public",
                table: "stock_reservation",
                columns: new[] { "variant_id", "status" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_balance",
                schema: "public");

            migrationBuilder.DropTable(
                name: "stock_ledger",
                schema: "public");

            migrationBuilder.DropTable(
                name: "stock_outbox_event",
                schema: "public");

            migrationBuilder.DropTable(
                name: "stock_reservation",
                schema: "public");

            migrationBuilder.DropTable(
                name: "stock_location",
                schema: "public");
        }
    }
}
