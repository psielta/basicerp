using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplicationBasic.Models.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required(ErrorMessage = "O código é obrigatório")]
        [Display(Name = "Código de Verificação")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "O código deve ter 6 dígitos")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "O código deve conter apenas números")]
        public string OtpCode { get; set; }

        public string UserEmail { get; set; }

        public string UserName { get; set; }

        public string UserImage { get; set; }

        public string OrganizationName { get; set; }

        public string ReturnUrl { get; set; }

        public bool CanResend { get; set; }

        public int ResendInSeconds { get; set; }
    }
}