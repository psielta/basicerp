using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationBasic.Models.ViewModels
{
    public class PasswordLoginViewModel
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; }

        public string UserEmail { get; set; }

        public string UserName { get; set; }

        public string UserImage { get; set; }

        public string OrganizationName { get; set; }

        public string ReturnUrl { get; set; }
    }
}