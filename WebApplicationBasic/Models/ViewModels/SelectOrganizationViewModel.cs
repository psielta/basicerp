using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using EntityFrameworkProject.Models;

namespace WebApplicationBasic.Models.ViewModels
{
    public class SelectOrganizationViewModel
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; }

        public string UserEmail { get; set; }

        public string UserImage { get; set; }

        [Required(ErrorMessage = "Selecione uma organização")]
        [Display(Name = "Organização")]
        public Guid OrganizationId { get; set; }

        public List<OrganizationInfo> Organizations { get; set; }

        [Display(Name = "Método de Login")]
        public LoginMethod Method { get; set; }

        public string ReturnUrl { get; set; }

        public bool HasPassword { get; set; }

        public class OrganizationInfo
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Role { get; set; }
            public string Logo { get; set; }
        }
    }

    public enum LoginMethod
    {
        Password,
        OTP
    }
}