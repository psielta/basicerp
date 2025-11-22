using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkProject.Migrations
{
    public partial class RemoveAtributeSpu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_template_attribute_value",
                schema: "public");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_product_template_attribute_value_attribute_value_id",
                schema: "public",
                table: "product_template_attribute_value",
                column: "attribute_value_id");
        }
    }
}
