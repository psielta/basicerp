using EntityFrameworkProject.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebApplicationBasic.App_Start
{
    public static class DependencyConfig
    {
        private static IServiceProvider _serviceProvider;

        public static void RegisterDependencies()
        {
            var services = new ServiceCollection();

            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString
                ?? "Host=localhost;Port=5432;Database=basic_db;Username=adm;Password=156879";

            services.AddScoped<ApplicationDbContext>(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(connectionString);
                return new ApplicationDbContext(optionsBuilder.Options);
            });

            services.AddTransient<WebApplicationBasic.Controllers.HomeController>();

            _serviceProvider = services.BuildServiceProvider();

            DependencyResolver.SetResolver(new ServiceProviderDependencyResolver(_serviceProvider));
            ControllerBuilder.Current.SetControllerFactory(new ServiceProviderControllerFactory(_serviceProvider));
        }

        public static IServiceProvider ServiceProvider => _serviceProvider;
    }

    public class ServiceProviderDependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderDependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        public System.Collections.Generic.IEnumerable<object> GetServices(Type serviceType)
        {
            return _serviceProvider.GetServices(serviceType);
        }
    }

    public class ServiceProviderControllerFactory : DefaultControllerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderControllerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            if (controllerType == null)
                return null;

            var controller = _serviceProvider.GetService(controllerType);
            if (controller != null)
                return (IController)controller;

            return base.GetControllerInstance(requestContext, controllerType);
        }
    }
}
