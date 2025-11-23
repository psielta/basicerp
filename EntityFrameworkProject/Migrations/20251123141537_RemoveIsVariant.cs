using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkProject.Migrations
{
    public partial class RemoveIsVariant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_variant",
                schema: "public",
                table: "product_attribute");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_variant",
                schema: "public",
                table: "product_attribute",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
