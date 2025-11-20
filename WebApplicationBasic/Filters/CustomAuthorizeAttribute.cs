using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using EntityFrameworkProject.Data;
using WebApplicationBasic.Services;

namespace WebApplicationBasic.Filters
{
    /// <summary>
    /// Atributo de autorização customizado que valida a sessão do usuário
    /// </summary>
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        private string[] _allowedRoles;

        /// <summary>
        /// Roles permitidas (na organização atual)
        /// </summary>
        public string OrganizationRoles { get; set; }

        /// <summary>
        /// Roles globais permitidas
        /// </summary>
        public string GlobalRoles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Primeiro, verificar autenticação básica
            if (!base.AuthorizeCore(httpContext))
                return false;

            var user = httpContext.User as ClaimsPrincipal;
            if (user == null || !user.Identity.IsAuthenticated)
                return false;

            // Obter serviços
            var sessionService = DependencyResolver.Current.GetService<ISessionService>();
            var context = DependencyResolver.Current.GetService<ApplicationDbContext>();

            // Validar sessão
            var sessionToken = user.Claims.FirstOrDefault(c => c.Type == "SessionToken")?.Value;
            if (string.IsNullOrEmpty(sessionToken))
                return false;

            var session = sessionService.GetSessionWithDetails(sessionToken);
            if (session == null)
                return false;

            // Verificar roles da organização
            if (!string.IsNullOrEmpty(OrganizationRoles))
            {
                var orgRoles = OrganizationRoles.Split(',').Select(r => r.Trim()).ToArray();
                var userOrgRole = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userOrgRole) || !orgRoles.Contains(userOrgRole, StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            // Verificar roles globais
            if (!string.IsNullOrEmpty(GlobalRoles))
            {
                var globalRoles = GlobalRoles.Split(',').Select(r => r.Trim()).ToArray();
                var userGlobalRole = user.Claims.FirstOrDefault(c => c.Type == "GlobalRole")?.Value;

                if (string.IsNullOrEmpty(userGlobalRole) || !globalRoles.Contains(userGlobalRole, StringComparer.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // Usuário autenticado mas sem permissão
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml"
                };
            }
            else
            {
                // Usuário não autenticado - redirecionar para login
                var returnUrl = filterContext.HttpContext.Request.Url?.PathAndQuery;
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Auth" },
                        { "action", "Login" },
                        { "returnUrl", returnUrl }
                    }
                );
            }
        }
    }
}