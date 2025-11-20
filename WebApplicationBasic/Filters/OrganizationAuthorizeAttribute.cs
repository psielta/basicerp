using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebApplicationBasic.Filters
{
    /// <summary>
    /// Atributo para verificar se o usuário tem permissão específica em uma organização
    /// </summary>
    public class OrganizationAuthorizeAttribute : CustomAuthorizeAttribute
    {
        private readonly string[] _requiredRoles;

        /// <summary>
        /// Cria um novo atributo de autorização de organização
        /// </summary>
        /// <param name="requiredRoles">Roles necessárias (owner, admin, member, etc)</param>
        public OrganizationAuthorizeAttribute(params string[] requiredRoles)
        {
            _requiredRoles = requiredRoles;
            OrganizationRoles = string.Join(",", requiredRoles);
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Verificar autorização básica
            if (!base.AuthorizeCore(httpContext))
                return false;

            // Verificar se tem organização selecionada
            var user = httpContext.User as ClaimsPrincipal;
            var organizationId = user?.Claims?.FirstOrDefault(c => c.Type == "OrganizationId")?.Value;

            if (string.IsNullOrEmpty(organizationId))
            {
                // Usuário não tem organização selecionada
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Requer que o usuário seja administrador da organização
    /// </summary>
    public class OrganizationAdminAttribute : OrganizationAuthorizeAttribute
    {
        public OrganizationAdminAttribute() : base("admin", "owner")
        {
        }
    }

    /// <summary>
    /// Requer que o usuário seja dono da organização
    /// </summary>
    public class OrganizationOwnerAttribute : OrganizationAuthorizeAttribute
    {
        public OrganizationOwnerAttribute() : base("owner")
        {
        }
    }

    /// <summary>
    /// Requer que o usuário seja membro de uma organização (qualquer role)
    /// </summary>
    public class OrganizationMemberAttribute : OrganizationAuthorizeAttribute
    {
        public OrganizationMemberAttribute() : base("owner", "admin", "member", "viewer")
        {
        }
    }
}