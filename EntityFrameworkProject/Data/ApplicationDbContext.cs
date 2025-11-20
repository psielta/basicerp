using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace EntityFrameworkProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasIndex(e => e.Cnpj).IsUnique();
                entity.HasIndex(e => e.Email);
            });

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.TenantId);

                entity.HasOne(u => u.Tenant)
                    .WithMany(t => t.Usuarios)
                    .HasForeignKey(u => u.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Cpf);
                entity.HasIndex(e => e.Cnpj);
                entity.HasIndex(e => e.TenantId);

                entity.HasOne(c => c.Tenant)
                    .WithMany()
                    .HasForeignKey(c => c.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenant>().HasData(
                new Tenant
                {
                    Id = 1,
                    Nome = "Tenant Padrão",
                    Cnpj = "00.000.000/0000-00",
                    Email = "contato@tenantpadrao.com",
                    Telefone = "(11) 9999-9999",
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    TenantId = 1,
                    Nome = "Administrador",
                    Email = "admin@tenantpadrao.com",
                    SenhaHash = "SenhaTemporaria123",
                    Role = "Admin",
                    Ativo = true,
                    DataCriacao = DateTime.UtcNow
                }
            );
        }
    }
}
