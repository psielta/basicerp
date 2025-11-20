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
            var totalTenants = _context.Tenants.Count();
            var totalUsuarios = _context.Usuarios.Count();
            var totalClientes = _context.Clientes.Count();

            ViewBag.TotalTenants = totalTenants;
            ViewBag.TotalUsuarios = totalUsuarios;
            ViewBag.TotalClientes = totalClientes;

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