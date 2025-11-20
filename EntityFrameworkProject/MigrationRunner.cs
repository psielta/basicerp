using EntityFrameworkProject.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace EntityFrameworkProject
{
    public class MigrationRunner
    {
        public static void Main(string[] args)
        {
            try
            {
                var factory = new ApplicationDbContextFactory();
                using (var context = factory.CreateDbContext(args))
                {
                    Console.WriteLine("Criando/atualizando banco de dados...");
                    context.Database.EnsureCreated();
                    Console.WriteLine("Banco de dados criado/atualizado com sucesso!");

                    Console.WriteLine("\nTabelas criadas:");
                    Console.WriteLine("- tenants");
                    Console.WriteLine("- usuarios");
                    Console.WriteLine("\nDados iniciais inseridos:");
                    Console.WriteLine("- 1 Tenant Padrão");
                    Console.WriteLine("- 1 Usuário Administrador");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
}
