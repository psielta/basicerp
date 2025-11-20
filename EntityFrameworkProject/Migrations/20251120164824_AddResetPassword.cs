using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EntityFrameworkProject.Migrations
{
    public partial class AddResetPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_password_change",
                schema: "public",
                table: "account",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reset_token",
                schema: "public",
                table: "account",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reset_token_expires",
                schema: "public",
                table: "account",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_password_change",
                schema: "public",
                table: "account");

            migrationBuilder.DropColumn(
                name: "reset_token",
                schema: "public",
                table: "account");

            migrationBuilder.DropColumn(
                name: "reset_token_expires",
                schema: "public",
                table: "account");
        }
    }
}
