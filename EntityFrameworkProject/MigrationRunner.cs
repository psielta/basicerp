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

                    Console.WriteLine("\nEstrutura principal:");
                    Console.WriteLine("- organization (tenants)");
                    Console.WriteLine("- user (usuarios globais)");
                    Console.WriteLine("- memberships (papel do usuario dentro da organization)");
                    Console.WriteLine("- account (credenciais/oauth)");
                    Console.WriteLine("- session (controle de sessao)");
                    Console.WriteLine("\nExecute suas migrations ou o script SQL para popular os dados.");
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
