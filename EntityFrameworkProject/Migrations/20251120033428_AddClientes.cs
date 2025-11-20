using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace EntityFrameworkProject.Migrations
{
    public partial class AddClientes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(nullable: false),
                    nome = table.Column<string>(maxLength: 100, nullable: false),
                    cpf = table.Column<string>(maxLength: 18, nullable: true),
                    cnpj = table.Column<string>(maxLength: 18, nullable: true),
                    email = table.Column<string>(maxLength: 200, nullable: true),
                    telefone = table.Column<string>(maxLength: 20, nullable: true),
                    celular = table.Column<string>(maxLength: 20, nullable: true),
                    cep = table.Column<string>(maxLength: 10, nullable: true),
                    endereco = table.Column<string>(maxLength: 200, nullable: true),
                    numero = table.Column<string>(maxLength: 10, nullable: true),
                    complemento = table.Column<string>(maxLength: 100, nullable: true),
                    bairro = table.Column<string>(maxLength: 100, nullable: true),
                    cidade = table.Column<string>(maxLength: 100, nullable: true),
                    estado = table.Column<string>(maxLength: 2, nullable: true),
                    ativo = table.Column<bool>(nullable: false),
                    data_criacao = table.Column<DateTime>(nullable: false),
                    data_atualizacao = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id);
                    table.ForeignKey(
                        name: "FK_clientes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "tenants",
                keyColumn: "id",
                keyValue: 1,
                column: "data_criacao",
                value: new DateTime(2025, 11, 20, 3, 34, 27, 964, DateTimeKind.Utc).AddTicks(5573));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: 1,
                column: "data_criacao",
                value: new DateTime(2025, 11, 20, 3, 34, 27, 965, DateTimeKind.Utc).AddTicks(5578));

            migrationBuilder.CreateIndex(
                name: "IX_clientes_cnpj",
                table: "clientes",
                column: "cnpj");

            migrationBuilder.CreateIndex(
                name: "IX_clientes_cpf",
                table: "clientes",
                column: "cpf");

            migrationBuilder.CreateIndex(
                name: "IX_clientes_email",
                table: "clientes",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_clientes_tenant_id",
                table: "clientes",
                column: "tenant_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.UpdateData(
                table: "tenants",
                keyColumn: "id",
                keyValue: 1,
                column: "data_criacao",
                value: new DateTime(2025, 11, 20, 3, 14, 47, 927, DateTimeKind.Utc).AddTicks(4667));

            migrationBuilder.UpdateData(
                table: "usuarios",
                keyColumn: "id",
                keyValue: 1,
                column: "data_criacao",
                value: new DateTime(2025, 11, 20, 3, 14, 47, 929, DateTimeKind.Utc).AddTicks(4639));
        }
    }
}
