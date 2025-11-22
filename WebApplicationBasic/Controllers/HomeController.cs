using EntityFrameworkProject.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplicationBasic.Filters;
using Serilog;

namespace WebApplicationBasic.Controllers
{
    public class RecentProductViewModel
    {
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public int VariantCount { get; set; }
    }

    [CustomAuthorize]
    public class HomeController : BaseController
    {
        public HomeController()
        {
        }

        public ActionResult Index()
        {
            Log.Debug("DASHBOARD_ACCESS: Usuário {UserId} acessando dashboard na organização {OrganizationId}",
                CurrentUserId, CurrentOrganizationId);

            // Estatísticas globais
            var totalOrganizations = Context.Organizations.Count();
            var totalUsers = Context.Users.Count();
            var totalMemberships = Context.Memberships.Count();

            ViewBag.TotalOrganizations = totalOrganizations;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalMemberships = totalMemberships;

            // Informações do usuário logado
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.UserOrganizations = Context.Memberships
                    .Where(m => m.UserId == CurrentUserId)
                    .Count();

                // Estatísticas da organização atual
                if (CurrentOrganizationId != Guid.Empty)
                {
                    // Produtos
                    var totalProducts = Context.ProductTemplates
                        .Count(p => p.OrganizationId == CurrentOrganizationId && p.DeletedAt == null);

                    var activeProducts = Context.ProductTemplates
                        .Where(p => p.OrganizationId == CurrentOrganizationId && p.DeletedAt == null)
                        .Count(p => p.Variants.Any(v => v.IsActive && v.DeletedAt == null));

                    var totalVariants = Context.ProductVariants
                        .Count(v => v.OrganizationId == CurrentOrganizationId && v.DeletedAt == null);

                    var activeVariants = Context.ProductVariants
                        .Count(v => v.OrganizationId == CurrentOrganizationId && v.IsActive && v.DeletedAt == null);

                    ViewBag.TotalProducts = totalProducts;
                    ViewBag.ActiveProducts = activeProducts;
                    ViewBag.TotalVariants = totalVariants;
                    ViewBag.ActiveVariants = activeVariants;

                    // Produtos por tipo
                    var productsWithVariants = Context.ProductTemplates
                        .Where(p => p.OrganizationId == CurrentOrganizationId && p.DeletedAt == null)
                        .Select(p => new
                        {
                            p.Id,
                            VariantCount = p.Variants.Count(v => v.DeletedAt == null)
                        })
                        .ToList();

                    var simpleProducts = productsWithVariants.Count(p => p.VariantCount == 1);
                    var configurableProducts = productsWithVariants.Count(p => p.VariantCount > 1);

                    ViewBag.SimpleProducts = simpleProducts;
                    ViewBag.ConfigurableProducts = configurableProducts;

                    // Categorias
                    var totalCategories = Context.Categories
                        .Count(c => c.OrganizationId == CurrentOrganizationId);

                    ViewBag.TotalCategories = totalCategories;

                    // Atributos
                    var totalAttributes = Context.ProductAttributes
                        .Count(a => a.OrganizationId == CurrentOrganizationId);

                    var variantAttributes = Context.ProductAttributes
                        .Count(a => a.OrganizationId == CurrentOrganizationId && a.IsVariant);

                    var descriptiveAttributes = totalAttributes - variantAttributes;

                    ViewBag.TotalAttributes = totalAttributes;
                    ViewBag.VariantAttributes = variantAttributes;
                    ViewBag.DescriptiveAttributes = descriptiveAttributes;

                    // Produtos recentes
                    var recentProducts = Context.ProductTemplates
                        .Where(p => p.OrganizationId == CurrentOrganizationId && p.DeletedAt == null)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(5)
                        .Select(p => new RecentProductViewModel
                        {
                            Name = p.Name,
                            CreatedAt = p.CreatedAt,
                            VariantCount = p.Variants.Count(v => v.DeletedAt == null)
                        })
                        .ToList();

                    ViewBag.RecentProducts = recentProducts;
                }
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
