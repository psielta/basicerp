using EntityFrameworkProject.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplicationBasic.Filters;

namespace WebApplicationBasic.Controllers
{
    [CustomAuthorize]
    public class HomeController : BaseController
    {
        public HomeController()
        {
        }

        public ActionResult Index()
        {
            var totalOrganizations = Context.Organizations.Count();
            var totalUsers = Context.Users.Count();
            var totalMemberships = Context.Memberships.Count();

            ViewBag.TotalOrganizations = totalOrganizations;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalMemberships = totalMemberships;

            // Informações adicionais do usuário logado
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.UserOrganizations = Context.Memberships
                    .Where(m => m.UserId == CurrentUserId)
                    .Count();
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
