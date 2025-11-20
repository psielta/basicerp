using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace EntityFrameworkProject.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(maxLength: 100, nullable: false),
                    cnpj = table.Column<string>(maxLength: 18, nullable: false),
                    email = table.Column<string>(maxLength: 200, nullable: true),
                    telefone = table.Column<string>(maxLength: 20, nullable: true),
                    ativo = table.Column<bool>(nullable: false),
                    data_criacao = table.Column<DateTime>(nullable: false),
                    data_atualizacao = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tenant_id = table.Column<int>(nullable: false),
                    nome = table.Column<string>(maxLength: 100, nullable: false),
                    email = table.Column<string>(maxLength: 200, nullable: false),
                    senha_hash = table.Column<string>(maxLength: 255, nullable: false),
                    role = table.Column<string>(maxLength: 50, nullable: true),
                    ativo = table.Column<bool>(nullable: false),
                    data_criacao = table.Column<DateTime>(nullable: false),
                    data_atualizacao = table.Column<DateTime>(nullable: true),
                    ultimo_login = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "FK_usuarios_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "tenants",
                columns: new[] { "id", "ativo", "cnpj", "data_atualizacao", "data_criacao", "email", "nome", "telefone" },
                values: new object[] { 1, true, "00.000.000/0000-00", null, new DateTime(2025, 11, 20, 3, 14, 47, 927, DateTimeKind.Utc).AddTicks(4667), "contato@tenantpadrao.com", "Tenant Padrão", "(11) 9999-9999" });

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "id", "ativo", "data_atualizacao", "data_criacao", "email", "nome", "role", "senha_hash", "tenant_id", "ultimo_login" },
                values: new object[] { 1, true, null, new DateTime(2025, 11, 20, 3, 14, 47, 929, DateTimeKind.Utc).AddTicks(4639), "admin@tenantpadrao.com", "Administrador", "Admin", "SenhaTemporaria123", 1, null });

            migrationBuilder.CreateIndex(
                name: "IX_tenants_cnpj",
                table: "tenants",
                column: "cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_email",
                table: "tenants",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_tenant_id",
                table: "usuarios",
                column: "tenant_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
