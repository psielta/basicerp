using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using EntityFrameworkProject.Data;
using Microsoft.Extensions.DependencyInjection;
using WebApplicationBasic.App_Start;
using WebApplicationBasic.Data;

namespace WebApplicationBasic
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            DependencyConfig.RegisterDependencies();

            // Configurar Anti-Forgery Token para funcionar com Claims
            AntiForgeryConfig.UniqueClaimTypeIdentifier = System.Security.Claims.ClaimTypes.NameIdentifier;

            // Inicializar banco de dados com dados de teste
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                using (var scope = DependencyConfig.ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Garantir que o banco de dados existe
                    context.Database.EnsureCreated();

                    // Popular com dados de teste
                    SeedData.Initialize(context);
                }
            }
            catch (Exception ex)
            {
                // Em produção, você deve logar o erro
                System.Diagnostics.Debug.WriteLine($"Erro ao inicializar banco de dados: {ex.Message}");
            }
        }
    }
}
