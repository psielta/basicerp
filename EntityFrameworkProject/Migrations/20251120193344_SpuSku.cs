using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkProject.Migrations
{
    public partial class SpuSku : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    path = table.Column<string>(type: "text", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.id);
                    table.ForeignKey(
                        name: "category_organization_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "category_parent_fk",
                        column: x => x.parent_id,
                        principalSchema: "public",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "product_attribute",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    is_variant = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_attribute", x => x.id);
                    table.ForeignKey(
                        name: "product_attribute_organization_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_template",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "citext", nullable: false),
                    brand = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    warranty_months = table.Column<int>(type: "integer", nullable: true),
                    is_service = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_rental = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    has_delivery = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ncm = table.Column<string>(type: "text", nullable: true),
                    nbs = table.Column<string>(type: "text", nullable: true),
                    freight_mode = table.Column<short>(type: "smallint", nullable: true),
                    aggregator_code = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_template", x => x.id);
                    table.ForeignKey(
                        name: "product_template_created_by_user_fk",
                        column: x => x.created_by_user_id,
                        principalSchema: "public",
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "product_template_organization_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_attribute_value",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    attribute_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_attribute_value", x => x.id);
                    table.ForeignKey(
                        name: "product_attribute_value_attribute_fk",
                        column: x => x.attribute_id,
                        principalSchema: "public",
                        principalTable: "product_attribute",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_template_category",
                schema: "public",
                columns: table => new
                {
                    product_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_template_category", x => new { x.product_template_id, x.category_id });
                    table.ForeignKey(
                        name: "product_template_category_category_fk",
                        column: x => x.category_id,
                        principalSchema: "public",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "product_template_category_template_fk",
                        column: x => x.product_template_id,
                        principalSchema: "public",
                        principalTable: "product_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variant",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    product_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    cost = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    weight = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    height = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    width = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    length = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    barcode = table.Column<string>(type: "text", nullable: true),
                    raw_variation_description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variant", x => x.id);
                    table.ForeignKey(
                        name: "product_variant_organization_fk",
                        column: x => x.organization_id,
                        principalSchema: "public",
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "product_variant_template_fk",
                        column: x => x.product_template_id,
                        principalSchema: "public",
                        principalTable: "product_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_template_attribute_value",
                schema: "public",
                columns: table => new
                {
                    product_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_template_attribute_value", x => new { x.product_template_id, x.attribute_value_id });
                    table.ForeignKey(
                        name: "product_template_attribute_value_value_fk",
                        column: x => x.attribute_value_id,
                        principalSchema: "public",
                        principalTable: "product_attribute_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "product_template_attribute_value_template_fk",
                        column: x => x.product_template_id,
                        principalSchema: "public",
                        principalTable: "product_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_image",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    product_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    url = table.Column<string>(type: "text", nullable: false),
                    alt_text = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_main = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_image", x => x.id);
                    table.ForeignKey(
                        name: "product_image_template_fk",
                        column: x => x.product_template_id,
                        principalSchema: "public",
                        principalTable: "product_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "product_image_variant_fk",
                        column: x => x.variant_id,
                        principalSchema: "public",
                        principalTable: "product_variant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variant_attribute_value",
                schema: "public",
                columns: table => new
                {
                    variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attribute_value_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variant_attribute_value", x => new { x.variant_id, x.attribute_value_id });
                    table.ForeignKey(
                        name: "product_variant_attribute_value_value_fk",
                        column: x => x.attribute_value_id,
                        principalSchema: "public",
                        principalTable: "product_attribute_value",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "product_variant_attribute_value_variant_fk",
                        column: x => x.variant_id,
                        principalSchema: "public",
                        principalTable: "product_variant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_org",
                schema: "public",
                table: "category",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_category_parent_id",
                schema: "public",
                table: "category",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_category_org_parent",
                schema: "public",
                table: "category",
                columns: new[] { "organization_id", "parent_id" });

            migrationBuilder.CreateIndex(
                name: "ux_category_org_slug",
                schema: "public",
                table: "category",
                columns: new[] { "organization_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_attribute_org",
                schema: "public",
                table: "product_attribute",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_attribute_org_code",
                schema: "public",
                table: "product_attribute",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_product_attribute_value_attribute_id",
                schema: "public",
                table: "product_attribute_value",
                column: "attribute_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_attribute_value",
                schema: "public",
                table: "product_attribute_value",
                columns: new[] { "attribute_id", "value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_product_image_main_template",
                schema: "public",
                table: "product_image",
                column: "product_template_id",
                unique: true,
                filter: "is_main = true AND variant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_product_image_variant",
                schema: "public",
                table: "product_image",
                column: "variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_template_created_by_user_id",
                schema: "public",
                table: "product_template",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_template_org",
                schema: "public",
                table: "product_template",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_template_org_aggregator",
                schema: "public",
                table: "product_template",
                columns: new[] { "organization_id", "aggregator_code" },
                unique: true,
                filter: "aggregator_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_product_template_org_slug",
                schema: "public",
                table: "product_template",
                columns: new[] { "organization_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_template_attribute_value_attribute_value_id",
                schema: "public",
                table: "product_template_attribute_value",
                column: "attribute_value_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_template_category_category_id",
                schema: "public",
                table: "product_template_category",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_product_variant_product_template_id",
                schema: "public",
                table: "product_variant",
                column: "product_template_id");

            migrationBuilder.CreateIndex(
                name: "ux_product_variant_org_sku",
                schema: "public",
                table: "product_variant",
                columns: new[] { "organization_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variant_attribute_value_attribute_value_id",
                schema: "public",
                table: "product_variant_attribute_value",
                column: "attribute_value_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_image",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_template_attribute_value",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_template_category",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_variant_attribute_value",
                schema: "public");

            migrationBuilder.DropTable(
                name: "category",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_attribute_value",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_variant",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_attribute",
                schema: "public");

            migrationBuilder.DropTable(
                name: "product_template",
                schema: "public");
        }
    }
}
