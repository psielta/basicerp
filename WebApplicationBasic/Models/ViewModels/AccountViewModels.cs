using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace WebApplicationBasic.Models.ViewModels
{
    public class AccountProfileViewModel
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool EmailVerified { get; set; }
        public string Image { get; set; }
        public string Role { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasPassword { get; set; }
        public DateTime? LastPasswordChange { get; set; }
        public List<OrganizationMembershipViewModel> Organizations { get; set; }
    }

    public class OrganizationMembershipViewModel
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string OrganizationSlug { get; set; }
        public string Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo {1} caracteres")]
        [Display(Name = "Nome")]
        public string Name { get; set; }

        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Foto de Perfil")]
        public HttpPostedFileBase ImageFile { get; set; }

        [Display(Name = "URL da Imagem (alternativa)")]
        [Url(ErrorMessage = "URL inválida")]
        public string ImageUrl { get; set; }

        // Campo oculto para manter a imagem atual
        public string CurrentImage { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "A senha atual é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha Atual")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "A nova senha é obrigatória")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "A senha deve ter pelo menos {2} caracteres.", MinimumLength = 8)]
        [Display(Name = "Nova Senha")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "A confirmação de senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Senha")]
        [Compare("NewPassword", ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmPassword { get; set; }
    }

    public class AccountSecurityViewModel
    {
        public List<SessionViewModel> Sessions { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }

    public class SessionViewModel
    {
        public Guid SessionId { get; set; }
        public string Token { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsCurrent { get; set; }
        public string OrganizationName { get; set; }

        public string DeviceInfo
        {
            get
            {
                if (string.IsNullOrEmpty(UserAgent))
                    return "Dispositivo desconhecido";

                if (UserAgent.Contains("Windows"))
                    return "Windows";
                else if (UserAgent.Contains("Mac"))
                    return "macOS";
                else if (UserAgent.Contains("Linux"))
                    return "Linux";
                else if (UserAgent.Contains("iPhone") || UserAgent.Contains("iPad"))
                    return "iOS";
                else if (UserAgent.Contains("Android"))
                    return "Android";
                else
                    return "Outro";
            }
        }

        public string BrowserInfo
        {
            get
            {
                if (string.IsNullOrEmpty(UserAgent))
                    return "Navegador desconhecido";

                if (UserAgent.Contains("Chrome"))
                    return "Chrome";
                else if (UserAgent.Contains("Firefox"))
                    return "Firefox";
                else if (UserAgent.Contains("Safari") && !UserAgent.Contains("Chrome"))
                    return "Safari";
                else if (UserAgent.Contains("Edge"))
                    return "Edge";
                else if (UserAgent.Contains("Opera"))
                    return "Opera";
                else
                    return "Outro";
            }
        }
    }

    public class AccountOrganizationsViewModel
    {
        public Guid CurrentOrganizationId { get; set; }
        public List<OrganizationDetailViewModel> Organizations { get; set; }
    }

    public class OrganizationDetailViewModel
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public string Logo { get; set; }
        public string Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsCurrent { get; set; }
        public int MemberCount { get; set; }
    }
}