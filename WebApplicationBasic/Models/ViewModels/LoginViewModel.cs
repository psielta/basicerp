using System.ComponentModel.DataAnnotations;

namespace WebApplicationBasic.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "O email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Lembrar-me")]
        public bool RememberMe { get; set; } = true;

        public string ReturnUrl { get; set; }
    }
}