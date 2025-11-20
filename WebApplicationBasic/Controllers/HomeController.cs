using EntityFrameworkProject.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplicationBasic.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public ActionResult Index()
        {
            var totalOrganizations = _context.Organizations.Count();
            var totalUsers = _context.Users.Count();
            var totalMemberships = _context.Memberships.Count();

            ViewBag.TotalOrganizations = totalOrganizations;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalMemberships = totalMemberships;

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
