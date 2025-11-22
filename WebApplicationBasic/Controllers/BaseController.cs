using System;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using EntityFrameworkProject.Data;
using EntityFrameworkProject.Models;
using Microsoft.EntityFrameworkCore;

namespace WebApplicationBasic.Controllers
{
    public abstract class BaseController : Controller
    {
        private ApplicationDbContext _context;
        private User _currentUser;
        private Organization _currentOrganization;
        private Membership _currentMembership;

        protected ApplicationDbContext Context
        {
            get
            {
                if (_context == null)
                {
                    _context = DependencyResolver.Current.GetService<ApplicationDbContext>();
                }
                return _context;
            }
        }

        /// <summary>
        /// ID do usuário atual logado
        /// </summary>
        protected Guid CurrentUserId
        {
            get
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                var claim = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (claim != null && Guid.TryParse(claim.Value, out var userId))
                {
                    return userId;
                }
                return Guid.Empty;
            }
        }

        /// <summary>
        /// ID da organização ativa do usuário
        /// </summary>
        public Guid CurrentOrganizationId
        {
            get
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                var claim = claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == "OrganizationId");
                if (claim != null && Guid.TryParse(claim.Value, out var orgId))
                {
                    return orgId;
                }
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Nome do usuário atual
        /// </summary>
        protected string CurrentUserName
        {
            get
            {
                return User?.Identity?.Name ?? string.Empty;
            }
        }

        /// <summary>
        /// Email do usuário atual
        /// </summary>
        protected string CurrentUserEmail
        {
            get
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                return claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty;
            }
        }

        /// <summary>
        /// Nome da organização ativa
        /// </summary>
        protected string CurrentOrganizationName
        {
            get
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                return claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == "OrganizationName")?.Value ?? string.Empty;
            }
        }

        /// <summary>
        /// Role do usuário na organização atual
        /// </summary>
        protected string CurrentOrganizationRole
        {
            get
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                return claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
            }
        }

        /// <summary>
        /// Role global do usuário (admin de sistema, etc)
        /// </summary>
        protected string CurrentGlobalRole
        {
            get
            {
                var claimsPrincipal = User as ClaimsPrincipal;
                return claimsPrincipal?.Claims?.FirstOrDefault(c => c.Type == "GlobalRole")?.Value ?? "user";
            }
        }

        /// <summary>
        /// Objeto completo do usuário atual
        /// </summary>
        protected User CurrentUser
        {
            get
            {
                if (_currentUser == null && CurrentUserId != Guid.Empty)
                {
                    _currentUser = Context.Users
                        .Include(u => u.Memberships)
                        .Include(u => u.Accounts)
                        .FirstOrDefault(u => u.Id == CurrentUserId);
                }
                return _currentUser;
            }
        }

        /// <summary>
        /// Objeto completo da organização ativa
        /// </summary>
        protected Organization CurrentOrganization
        {
            get
            {
                if (_currentOrganization == null && CurrentOrganizationId != Guid.Empty)
                {
                    _currentOrganization = Context.Organizations
                        .Include(o => o.Memberships)
                        .FirstOrDefault(o => o.Id == CurrentOrganizationId);
                }
                return _currentOrganization;
            }
        }

        /// <summary>
        /// Membership do usuário na organização atual
        /// </summary>
        protected Membership CurrentMembership
        {
            get
            {
                if (_currentMembership == null && CurrentUserId != Guid.Empty && CurrentOrganizationId != Guid.Empty)
                {
                    _currentMembership = Context.Memberships
                        .Include(m => m.User)
                        .Include(m => m.Organization)
                        .FirstOrDefault(m => m.UserId == CurrentUserId && m.OrganizationId == CurrentOrganizationId);
                }
                return _currentMembership;
            }
        }

        /// <summary>
        /// Verifica se o usuário tem uma role específica na organização
        /// </summary>
        protected bool UserHasOrganizationRole(params string[] roles)
        {
            if (string.IsNullOrEmpty(CurrentOrganizationRole))
                return false;

            return roles.Any(r => r.Equals(CurrentOrganizationRole, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verifica se o usuário tem uma role global específica
        /// </summary>
        protected bool UserHasGlobalRole(params string[] roles)
        {
            if (string.IsNullOrEmpty(CurrentGlobalRole))
                return false;

            return roles.Any(r => r.Equals(CurrentGlobalRole, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Verifica se o usuário é administrador da organização atual
        /// </summary>
        protected bool IsOrganizationAdmin
        {
            get
            {
                return UserHasOrganizationRole("admin", "owner");
            }
        }

        /// <summary>
        /// Verifica se o usuário é administrador global do sistema
        /// </summary>
        protected bool IsGlobalAdmin
        {
            get
            {
                return UserHasGlobalRole("admin", "super_admin");
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Disponibilizar informações do usuário para as Views
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.CurrentUserId = CurrentUserId;
                ViewBag.CurrentUserName = CurrentUserName;
                ViewBag.CurrentUserEmail = CurrentUserEmail;
                ViewBag.CurrentUserImage = CurrentUser?.Image;
                ViewBag.CurrentOrganizationId = CurrentOrganizationId;
                ViewBag.CurrentOrganizationName = CurrentOrganizationName;
                ViewBag.CurrentOrganizationRole = CurrentOrganizationRole;
                ViewBag.IsOrganizationAdmin = IsOrganizationAdmin;
                ViewBag.IsGlobalAdmin = IsGlobalAdmin;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}