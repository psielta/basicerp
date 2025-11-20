using System;
using System.Linq;
using EntityFrameworkProject.Data;
using EntityFrameworkProject.Models;
using WebApplicationBasic.Services;

namespace WebApplicationBasic.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Verificar se já existem dados
            if (context.Users.Any())
            {
                return; // DB já foi populado
            }

            var passwordHasher = new PasswordHasher();

            // Criar organizações
            var org1 = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Empresa Exemplo",
                Slug = "empresa-exemplo",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = "{}"
            };

            var org2 = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Startup Tech",
                Slug = "startup-tech",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = "{}"
            };

            context.Organizations.AddRange(org1, org2);

            // Criar usuários
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "Admin User",
                Email = "admin@example.com",
                EmailVerified = true,
                Role = "admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = "{}"
            };

            var normalUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "João Silva",
                Email = "joao@example.com",
                EmailVerified = true,
                Role = "user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = "{}"
            };

            var multiOrgUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "Maria Santos",
                Email = "maria@example.com",
                EmailVerified = true,
                Role = "user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Metadata = "{}"
            };

            context.Users.AddRange(adminUser, normalUser, multiOrgUser);

            // Criar contas locais (senhas)
            var adminAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                AccountId = adminUser.Id.ToString(),
                ProviderId = "local",
                Password = passwordHasher.HashPassword("admin123"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var normalAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = normalUser.Id,
                AccountId = normalUser.Id.ToString(),
                ProviderId = "local",
                Password = passwordHasher.HashPassword("senha123"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var multiOrgAccount = new Account
            {
                Id = Guid.NewGuid(),
                UserId = multiOrgUser.Id,
                AccountId = multiOrgUser.Id.ToString(),
                ProviderId = "local",
                Password = passwordHasher.HashPassword("maria123"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Accounts.AddRange(adminAccount, normalAccount, multiOrgAccount);

            // Criar memberships (vincular usuários às organizações)
            var membership1 = new Membership
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                OrganizationId = org1.Id,
                Role = "owner",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var membership2 = new Membership
            {
                Id = Guid.NewGuid(),
                UserId = normalUser.Id,
                OrganizationId = org1.Id,
                Role = "member",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Maria tem acesso às duas organizações
            var membership3 = new Membership
            {
                Id = Guid.NewGuid(),
                UserId = multiOrgUser.Id,
                OrganizationId = org1.Id,
                Role = "admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var membership4 = new Membership
            {
                Id = Guid.NewGuid(),
                UserId = multiOrgUser.Id,
                OrganizationId = org2.Id,
                Role = "owner",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Memberships.AddRange(membership1, membership2, membership3, membership4);

            // Salvar tudo no banco
            context.SaveChanges();
        }
    }
}