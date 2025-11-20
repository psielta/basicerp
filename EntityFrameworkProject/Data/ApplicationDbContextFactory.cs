using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Configuration;

namespace EntityFrameworkProject.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Tenta obter a connection string do config ou usa o padrão
            string connectionString;

            try
            {
                var connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"];
                connectionString = connStr?.ConnectionString ?? GetDefaultConnectionString();
            }
            catch
            {
                connectionString = GetDefaultConnectionString();
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }

        private string GetDefaultConnectionString()
        {
            return "Host=localhost;Port=5432;Database=basic_db;Username=adm;Password=156879";
        }
    }
}
